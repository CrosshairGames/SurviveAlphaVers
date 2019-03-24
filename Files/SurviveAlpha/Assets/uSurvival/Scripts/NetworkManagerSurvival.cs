// We use a custom NetworkManager that also takes care of login, character
// selection, character creation and more.
//
// We don't use the playerPrefab, instead all available player classes should be
// dragged into the spawnable objects property.
//
using UnityEngine;
using Mirror;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

// we need a clearly defined state to know if we are offline/in world/in lobby
// otherwise UICharacterSelection etc. never know 100% if they should be visible
// or not.
public enum NetworkState {Offline, Handshake, Lobby, World}

public class NetworkManagerSurvival : NetworkManager
{
    // current network manager state on client
    public NetworkState state = NetworkState.Offline;

    // <conn, account> dict for the lobby
    // (people that are still creating or selecting characters)
    Dictionary<NetworkConnection, string> lobby = new Dictionary<NetworkConnection, string>();

    // UI components to avoid FindObjectOfType
    [Header("UI")]
    public UIPopup uiPopup;

    // login info for the local player
    // we don't just name it 'account' to avoid collisions in handshake
    [Header("Login")]
    public string loginAccount = "";
    public string loginPassword = "";

    // we may want to add another game server if the first one gets too crowded.
    // the server list allows people to choose a server.
    //
    // note: we use one port for all servers, so that a headless server knows
    // which port to bind to. otherwise it would have to know which one to
    // choose from the list, which is far too complicated. one port for all
    // servers will do just fine for an Indie game.
    [Serializable]
    public class ServerInfo
    {
        public string name;
        public string ip;
    }
    public List<ServerInfo> serverList = new List<ServerInfo>()
    {
        new ServerInfo{name="Local", ip="localhost"}
    };

    [Header("Database")]
    public int characterLimit = 4;
    public int characterNameMaxLength = 16;
    public int accountMaxLength = 16;
    public float saveInterval = 60f; // in seconds

    // store characters available message on client so that UI can access it
    [HideInInspector] public CharactersAvailableMsg charactersAvailableMsg;

    // name checks /////////////////////////////////////////////////////////////
    public bool IsAllowedAccountName(string account)
    {
        // not too long?
        // only contains letters, number and underscore and not empty (+)?
        // (important for database safety etc.)
        return account.Length <= accountMaxLength &&
               Regex.IsMatch(account, @"^[a-zA-Z0-9_]+$");
    }

    public bool IsAllowedCharacterName(string characterName)
    {
        // not too long?
        // only contains letters, number and underscore and not empty (+)?
        // (important for database safety etc.)
        return characterName.Length <= characterNameMaxLength &&
               Regex.IsMatch(characterName, @"^[a-zA-Z0-9_]+$");
    }

    // events //////////////////////////////////////////////////////////////////
    void Update()
    {
        // any valid local player? then set state to world
        if (ClientScene.localPlayer != null)
            state = NetworkState.World;
    }

    // error messages //////////////////////////////////////////////////////////
    void ServerSendError(NetworkConnection conn, string error, bool disconnect)
    {
        conn.Send(new ErrorMsg{text=error, causesDisconnect=disconnect});
    }

    void OnClientError(NetworkConnection conn, ErrorMsg message)
    {
        print("OnClientError: " + message.text);

        // show a popup
        uiPopup.Show(message.text);

        // disconnect if it was an important network error
        // (this is needed because the login failure message doesn't disconnect
        //  the client immediately (only after timeout))
        if (message.causesDisconnect)
        {
            conn.Disconnect();

            // also stop the host if running as host
            // (host shouldn't start server but disconnect client for invalid
            //  login, which would be pointless)
            if (NetworkServer.active) StopHost();
        }
    }

    // start & stop ////////////////////////////////////////////////////////////
    public override void OnStartServer()
    {
        // handshake packet handlers
        NetworkServer.RegisterHandler<LoginMsg>(OnServerLogin);
        NetworkServer.RegisterHandler<CharacterCreateMsg>(OnServerCharacterCreate);
        NetworkServer.RegisterHandler<CharacterDeleteMsg>(OnServerCharacterDelete);

        // call base function to guarantee proper functionality
        base.OnStartServer();

        // load all player generated structures
        // (after base.OnStartServer so that we can call NetworkServer.Spawn)
        Database.LoadStructures();

        // invoke saving
        InvokeRepeating(nameof(Save), saveInterval, saveInterval);
    }

    public override void OnStopServer()
    {
        print("OnStopServer");
        CancelInvoke(nameof(Save));

        // call base function to guarantee proper functionality
        base.OnStopServer();
    }

    // handshake: login ////////////////////////////////////////////////////////
    public bool IsConnecting()
    {
        return NetworkClient.active && !ClientScene.ready;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        print("OnClientConnect");

        // setup handlers
        client.RegisterHandler<CharactersAvailableMsg>(OnClientCharactersAvailable);
        client.RegisterHandler<ErrorMsg>(OnClientError);

        // send login packet with hashed password, so that the original one
        // never leaves the player's computer.
        //
        // it's recommended to use a different salt for each hash. ideally we
        // would store each user's salt in the database. to not overcomplicate
        // things, we will use the account name as salt (at least 16 bytes)
        //
        // Application.version can be modified under:
        // Edit -> Project Settings -> Player -> Bundle Version
        string hash = Utils.PBKDF2Hash(loginPassword, "at_least_16_byte" + loginAccount);
        LoginMsg message = new LoginMsg{account=loginAccount, password=hash, version=Application.version};
        conn.Send(message);
        print("login message was sent");

        // set state
        state = NetworkState.Handshake;

        // call base function to make sure that client becomes "ready"
        //base.OnClientConnect(conn);
        ClientScene.Ready(conn); // from bitbucket OnClientConnect source
    }

    // the default OnClientSceneChanged sets the client as ready automatically,
    // which makes no sense for our situation. this was more for situations
    // where the server tells all clients to load a new scene.
    // -> setting client as ready will cause 'already set as ready' errors if
    //    we call StartClient before loading a new scene (e.g. for zones)
    // -> it's best to just overwrite this with an empty function
    public override void OnClientSceneChanged(NetworkConnection conn) {}

    bool AccountLoggedIn(string account)
    {
        // in lobby or in world?
        return lobby.ContainsValue(account) ||
               PlayerMeta.onlinePlayers.Values.Any(p => p.GetComponent<PlayerMeta>().account == account);
    }

    // helper function to make a CharactersAvailableMsg from all characters in
    // an account
    CharactersAvailableMsg MakeCharactersAvailableMessage(string account)
    {
        // load from database
        List<GameObject> characters = Database.CharactersForAccount(account)
                                      .Select(character => Database.CharacterLoad(character, GetPlayerClasses()))
                                      .ToList();

        // construct the message
        CharactersAvailableMsg message = new CharactersAvailableMsg();
        message.Load(characters);

        // destroy the temporary players again and return the result
        characters.ForEach(player => Destroy(player.gameObject));
        return message;
    }

    void OnServerLogin(NetworkConnection conn, LoginMsg message)
    {
        //print("OnServerLogin " + conn);

        // correct version?
        if (message.version == Application.version)
        {
            // allowed account name?
            if (IsAllowedAccountName(message.account))
            {
                // validate account info
                if (Database.IsValidAccount(message.account, message.password))
                {
                    // not in lobby and not in world yet?
                    if (!AccountLoggedIn(message.account))
                    {
                        print("login successful: " + message.account);

                        // add to logged in accounts
                        lobby[conn] = message.account;

                        // send necessary data to client
                        conn.Send(MakeCharactersAvailableMessage(message.account));
                    }
                    else
                    {
                        print("account already logged in: " + message.account);
                        ServerSendError(conn, "already logged in", true);

                        // note: we should disconnect the client here, but we can't as
                        // long as unity has no "SendAllAndThenDisconnect" function,
                        // because then the error message would never be sent.
                        //netMsg.conn.Disconnect();
                    }
                }
                else
                {
                    print("invalid account or password for: " + message.account);
                    ServerSendError(conn, "invalid account", true);
                }
            }
            else
            {
                print("account name not allowed: " + message.account);
                ServerSendError(conn, "account name not allowed", true);
            }
        }
        else
        {
            print("version mismatch: " + message.account + " expected:" + Application.version + " received: " + message.version);
            ServerSendError(conn, "outdated version", true);
        }
    }

    // handshake: character selection //////////////////////////////////////////
    void OnClientCharactersAvailable(NetworkConnection conn, CharactersAvailableMsg message)
    {
        charactersAvailableMsg = message;
        print("characters available:" + charactersAvailableMsg.characters.Length);

        // set state
        state = NetworkState.Lobby;
    }

    // called after the client calls ClientScene.AddPlayer with a msg parameter
    public override void OnServerAddPlayer(NetworkConnection conn, AddPlayerMessage message)
    {
        //print("OnServerAddPlayer extra");
        // only while in lobby (aka after handshake and not ingame)
        if (lobby.ContainsKey(conn) && message.value.Length == sizeof(int))
        {
            // read the index and find the n-th character
            // (only if we know that he is not ingame, otherwise lobby has
            //  no netMsg.conn key)
            int index = BitConverter.ToInt32(message.value, 0);
            string account = lobby[conn];
            List<string> characters = Database.CharactersForAccount(account);

            // validate index
            if (0 <= index && index < characters.Count)
            {
                print(account + " selected player " + characters[index]);

                // load character data
                GameObject go = Database.CharacterLoad(characters[index], GetPlayerClasses());

                // add to client
                NetworkServer.AddPlayerForConnection(conn, go);

                // remove from lobby
                lobby.Remove(conn);
            }
            else
            {
                print("invalid character index: " + account + " " + index);
                ServerSendError(conn, "invalid character index", false);
            }
        }
        else
        {
            print("AddPlayer: not in lobby" + conn);
            ServerSendError(conn, "AddPlayer: not in lobby", true);
        }
    }

    // handshake: character creation ///////////////////////////////////////////
    // find all available player classes
    public List<GameObject> GetPlayerClasses()
    {
        return spawnPrefabs.Where(go => go.GetComponent<PlayerMeta>() != null).ToList();
    }

    void OnServerCharacterCreate(NetworkConnection conn, CharacterCreateMsg message)
    {
        //print("OnServerCharacterCreate " + conn);

        // only while in lobby (aka after handshake and not ingame)
        if (lobby.ContainsKey(conn))
        {
            // allowed character name?
            if (IsAllowedCharacterName(message.name))
            {
                // not existant yet?
                string account = lobby[conn];
                if (!Database.CharacterExists(message.name))
                {
                    // not too may characters created yet?
                    if (Database.CharactersForAccount(account).Count < characterLimit)
                    {
                        // valid class index?
                        List<GameObject> classes = GetPlayerClasses();
                        if (0 <= message.classIndex && message.classIndex < classes.Count)
                        {
                            // create new character based on the prefab.
                            // -> we also assign default items and equipment for new characters
                            // (instantiate temporary player)
                            print("creating character: " + message.name + " " + message.classIndex);
                            GameObject player = GameObject.Instantiate(classes[message.classIndex]);
                            player.name = message.name;
                            player.GetComponent<PlayerMeta>().account = account;
                            player.GetComponent<PlayerMeta>().className = classes[message.classIndex].name;
                            player.transform.position = GetStartPosition().position;
                            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                            for (int i = 0; i < inventory.size; ++i)
                            {
                                // add empty slot or default item if any
                                inventory.slots.Add(i < inventory.defaultItems.Length ? new ItemSlot(new Item(inventory.defaultItems[i])) : new ItemSlot());
                            }
                            PlayerEquipment equipment = player.GetComponent<PlayerEquipment>();
                            for (int i = 0; i < equipment.slotInfo.Length; ++i)
                            {
                                // add empty slot or default item if any
                                EquipmentInfo info = equipment.slotInfo[i];
                                equipment.slots.Add(info.defaultItem != null ? new ItemSlot( new Item(info.defaultItem)) : new ItemSlot());
                            }
                            PlayerHotbar hotbar = player.GetComponent<PlayerHotbar>();
                            for (int i = 0; i < hotbar.size; ++i)
                            {
                                // add empty slot or default item if any
                                hotbar.slots.Add(i < hotbar.defaultItems.Length ? new ItemSlot(new Item(hotbar.defaultItems[i])) : new ItemSlot());
                            }
                            // fill all energies (after equipment in case of boni)
                            foreach (Energy energy in player.GetComponents<Energy>())
                                energy.current = energy.max;

                            // save the player
                            Database.CharacterSave(player, false);
                            GameObject.Destroy(player);

                            // send available characters list again, causing
                            // the client to switch to the character
                            // selection scene again
                            conn.Send(MakeCharactersAvailableMessage(account));
                        }
                        else
                        {
                            print("character invalid class: " + message.classIndex);
                            ServerSendError(conn, "character invalid class", false);
                        }
                    }
                    else
                    {
                        print("character limit reached: " + message.name);
                        ServerSendError(conn, "character limit reached", false);
                    }
                }
                else
                {
                    print("character name already exists: " + message.name);
                    ServerSendError(conn, "name already exists", false);
                }
            }
            else
            {
                print("character name not allowed: " + message.name);
                ServerSendError(conn, "character name not allowed", false);
            }
        }
        else
        {
            print("CharacterCreate: not in lobby");
            ServerSendError(conn, "CharacterCreate: not in lobby", true);
        }
    }

    void OnServerCharacterDelete(NetworkConnection conn, CharacterDeleteMsg message)
    {
        //print("OnServerCharacterDelete " + conn);

        // only while in lobby (aka after handshake and not ingame)
        if (lobby.ContainsKey(conn))
        {
            string account = lobby[conn];
            List<string> characters = Database.CharactersForAccount(account);

            // validate index
            if (0 <= message.value && message.value < characters.Count)
            {
                // delete the character
                print("delete character: " + characters[message.value]);
                Database.CharacterDelete(characters[message.value]);

                // send the new character list to client
                conn.Send(MakeCharactersAvailableMessage(account));
            }
            else
            {
                print("invalid character index: " + account + " " + message.value);
                ServerSendError(conn, "invalid character index", false);
            }
        }
        else
        {
            print("CharacterDelete: not in lobby: " + conn);
            ServerSendError(conn, "CharacterDelete: not in lobby", true);
        }
    }

    // saving /////////////////////////////////////////////////////////////////
    void Save()
    {
        // we have to save all players at once to make sure that item trading is
        // perfectly save. if we would invoke a save function every few minutes on
        // each player seperately then it could happen that two players trade items
        // and only one of them is saved before a server crash - hence causing item
        // duplicates.
        List<GameObject> players = PlayerMeta.onlinePlayers.Values.ToList();
        Database.CharacterSaveMany(players);
        if (players.Count > 0) Debug.Log("saved " + players.Count + " player(s)");

        // save storages
        List<Storage> storages = Storage.storages.Values.ToList();
        Database.SaveStorages(storages);
        if (storages.Count > 0) Debug.Log("saved " + storages.Count + " storage(s)");

        // save player generated structures
        List<GameObject> structures = GameObject.FindGameObjectsWithTag("Structure").ToList();
        Database.SaveStructures(structures);
        if (structures.Count > 0) Debug.Log("saved " + structures.Count + " structure(s)");
    }

    // stop/disconnect /////////////////////////////////////////////////////////
    // called on the server when a client disconnects
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        print("OnServerDisconnect " + conn);

        // save player (if any)
        if (conn.playerController != null)
        {
            Database.CharacterSave(conn.playerController.gameObject, false);
            print("saved:" + conn.playerController.name);
        } else print("no player to save for: " + conn);

        // remove logged in account after everything else was done
        lobby.Remove(conn); // just returns false if not found

        // do base function logic (removes the player for the connection)
        base.OnServerDisconnect(conn);
    }

    // called on the client if he disconnects
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        print("OnClientDisconnect");

        // take the camera out of the local player so it doesn't get destroyed
        if (Camera.main.transform.parent != null)
            Camera.main.transform.SetParent(null);

        // show a popup so that users know what happened
        uiPopup.Show("Disconnected.");

        // call base function to guarantee proper functionality
        base.OnClientDisconnect(conn);

        // call StopClient to clean everything up properly (otherwise
        // NetworkClient.active remains false after next login)
        StopClient();

        // set state
        state = NetworkState.Offline;
    }

    // universal quit function for editor & build
    public static void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // called when quitting the application by closing the window / pressing
    // stop in the editor
    // -> we want to send the quit packet to the server instead of waiting for a
    //    timeout
    // -> this also avoids the OnDisconnectError UNET bug (#838689) more often
    new void OnApplicationQuit()
    {
        if (IsClientConnected())
        {
            StopClient();
            print("OnApplicationQuit: stopped client");
        }
        base.OnApplicationQuit();
    }

    new void OnValidate()
    {
        base.OnValidate();

        // ip has to be changed in the server list. make it obvious to users.
        if (!Application.isPlaying && networkAddress != "")
            networkAddress = "Use the Server List below!";
    }
}
