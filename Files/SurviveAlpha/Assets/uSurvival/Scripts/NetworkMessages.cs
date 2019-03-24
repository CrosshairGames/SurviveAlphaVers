// Contains all the network messages that we need.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

// client to server ////////////////////////////////////////////////////////////
public class LoginMsg : MessageBase
{
    public string account;
    public string password;
    public string version;
}

public class CharacterDeleteMsg : IntegerMessage {}

public class CharacterCreateMsg : MessageBase
{
    public string name;
    public int classIndex;
}

// server to client ////////////////////////////////////////////////////////////
// we need an error msg packet because we can't use TargetRpc with the Network-
// Manager, since it's not a MonoBehaviour.
public class ErrorMsg : MessageBase
{
    public string text;
    public bool causesDisconnect;
}

public class CharactersAvailableMsg : MessageBase
{
    public struct CharacterPreview
    {
        public string name;
        public string className; // = the prefab name
    }
    public CharacterPreview[] characters;

    // load method in this class so we can still modify the characters structs
    // in the addon hooks
    public void Load(List<GameObject> players)
    {
        // we only need name and class for our UI
        characters = players.Select(
            player => new CharacterPreview{
                name = player.name,
                className = player.GetComponent<PlayerMeta>().className
            }
        ).ToArray();
    }
}