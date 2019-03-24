// Saves Character Data in a SQLite database. We use SQLite for serveral reasons
//
// - SQLite is file based and works without having to setup a database server
//   - We can 'remove all ...' or 'modify all ...' easily via SQL queries
//   - A lot of people requested a SQL database and weren't comfortable with XML
//   - We can allow all kinds of character names, even chinese ones without
//     breaking the file system.
// - It's very easy to switch to MYSQL if a real database server is needed later
//
// Tools to open sqlite database files:
//   Windows/OSX program: http://sqlitebrowser.org/
//   Firefox extension: https://addons.mozilla.org/de/firefox/addon/sqlite-manager/
//   Webhost: Adminer/PhpLiteAdmin
//
// About performance:
// - It's recommended to only keep the SQlite connection open while it's used.
//   MMO Servers use it all the time, so we keep it open all the time. This also
//   allows us to use transactions easily, and it will make the transition to
//   MYSQL easier.
// - Transactions are definitely necessary:
//   saving 100 players without transactions takes 3.6s
//   saving 100 players with transactions takes    0.38s
// - Using tr = conn.BeginTransaction() + tr.Commit() and passing it through all
//   the functions is ultra complicated. We use a BEGIN + END queries instead.
//
// Some benchmarks:
//   saving 100 players unoptimized: 4s
//   saving 100 players always open connection + transactions: 3.6s
//   saving 100 players always open connection + transactions + WAL: 3.6s
//   saving 100 players in 1 'using tr = ...' transaction: 380ms
//   saving 100 players in 1 BEGIN/END style transactions: 380ms
//   saving 100 players with XML: 369ms
//
// Build notes:
// - requires Player settings to be set to '.NET' instead of '.NET Subset',
//   otherwise System.Data.dll causes ArgumentException.
// - requires sqlite3.dll x86 and x64 version for standalone (windows/mac/linux)
//   => found on sqlite.org website
// - requires libsqlite3.so x86 and armeabi-v7a for android
//   => compiled from sqlite.org amalgamation source with android ndk r9b linux
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using Mono.Data.Sqlite; // copied from Unity/Mono/lib/mono/2.0 to Plugins

public class Database
{
    // database path: Application.dataPath is always relative to the project,
    // but we don't want it inside the Assets folder in the Editor (git etc.),
    // instead we put it above that.
    // we also use Path.Combine for platform independent paths
    // and we need persistentDataPath on android
#if UNITY_EDITOR
    static string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Database.sqlite");
#elif UNITY_ANDROID
    static string path = Path.Combine(Application.persistentDataPath, "Database.sqlite");
#elif UNITY_IOS
    static string path = Path.Combine(Application.persistentDataPath, "Database.sqlite");
#else
    static string path = Path.Combine(Application.dataPath, "Database.sqlite");
#endif

    static SqliteConnection connection;

    // constructor /////////////////////////////////////////////////////////////
    static Database()
    {
        // create database file if it doesn't exist yet
        if(!File.Exists(path))
            SqliteConnection.CreateFile(path);

        // open connection
        connection = new SqliteConnection("URI=file:" + path);
        connection.Open();

        // create tables if they don't exist yet or were deleted
        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS characters (
                            name TEXT NOT NULL PRIMARY KEY,
                            account TEXT NOT NULL,
                            class TEXT NOT NULL,
                            x REAL NOT NULL,
                            y REAL NOT NULL,
                            z REAL NOT NULL,
                            health INTEGER NOT NULL,
                            hydration INTEGER NOT NULL,
                            nutrition INTEGER NOT NULL,
                            temperature INTEGER NOT NULL,
                            endurance INTEGER NOT NULL,
                            online TEXT NOT NULL,
                            deleted INTEGER NOT NULL)");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_inventory (
                            character TEXT NOT NULL,
                            slot INTEGER NOT NULL,
                            name TEXT NOT NULL,
                            amount INTEGER NOT NULL,
                            ammo INTEGER NOT NULL,
                            durability INTEGER NOT NULL,
                            PRIMARY KEY(character, slot))");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_equipment (
                            character TEXT NOT NULL,
                            slot INTEGER NOT NULL,
                            name TEXT NOT NULL,
                            amount INTEGER NOT NULL,
                            ammo INTEGER NOT NULL,
                            durability INTEGER NOT NULL,
                            PRIMARY KEY(character, slot))");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_hotbar (
                            character TEXT NOT NULL,
                            slot INTEGER NOT NULL,
                            name TEXT NOT NULL,
                            amount INTEGER NOT NULL,
                            ammo INTEGER NOT NULL,
                            durability INTEGER NOT NULL,
                            PRIMARY KEY(character, slot))");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS accounts (
                            name TEXT NOT NULL PRIMARY KEY,
                            password TEXT NOT NULL,
                            banned INTEGER NOT NULL)");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS storages (
                            storage TEXT NOT NULL,
                            slot INTEGER NOT NULL,
                            name TEXT NOT NULL,
                            amount INTEGER NOT NULL,
                            ammo INTEGER NOT NULL,
                            durability INTEGER NOT NULL,
                            PRIMARY KEY(storage, slot))");

        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS structures (
                            name TEXT NOT NULL,
                            x REAL NOT NULL,
                            y REAL NOT NULL,
                            z REAL NOT NULL,
                            xRot REAL NOT NULL,
                            yRot REAL NOT NULL,
                            zRot REAL NOT NULL)");

        Debug.Log("connected to database");
    }

    // helper functions ////////////////////////////////////////////////////////
    // run a query that doesn't return anything
    public static void ExecuteNonQuery(string sql, params SqliteParameter[] args)
    {
        using (SqliteCommand command = new SqliteCommand(sql, connection))
        {
            foreach (SqliteParameter param in args)
                command.Parameters.Add(param);
            command.ExecuteNonQuery();
        }
    }

    // run a query that returns a single value
    public static object ExecuteScalar(string sql, params SqliteParameter[] args)
    {
        using (SqliteCommand command = new SqliteCommand(sql, connection))
        {
            foreach (SqliteParameter param in args)
                command.Parameters.Add(param);
            return command.ExecuteScalar();
        }
    }

    // run a query that returns several values
    // note: sqlite has long instead of int, so use Convert.ToInt32 etc.
    public static List< List<object> > ExecuteReader(string sql, params SqliteParameter[] args)
    {
        List< List<object> > result = new List< List<object> >();

        using (SqliteCommand command = new SqliteCommand(sql, connection))
        {
            foreach (SqliteParameter param in args)
                command.Parameters.Add(param);

            using (SqliteDataReader reader = command.ExecuteReader())
            {
                // the following code causes a SQL EntryPointNotFoundException
                // because sqlite3_column_origin_name isn't found on OSX and
                // some other platforms. newer mono versions have a workaround,
                // but as long as Unity doesn't update, we will have to work
                // around it manually. see also GetSchemaTable function:
                // https://github.com/mono/mono/blob/master/mcs/class/Mono.Data.Sqlite/Mono.Data.Sqlite_2.0/SQLiteDataReader.cs
                //
                //result.Load(reader); (DataTable)
                //
                // UPDATE: DataTable.Load(reader) works in Net 4.X now, but it's
                //         20x slower than the current approach.
                //         select * from character_inventory x 1000:
                //           425ms before
                //          7303ms with DataRow
                while (reader.Read())
                {
                    object[] buffer = new object[reader.FieldCount];
                    reader.GetValues(buffer);
                    result.Add(buffer.ToList());
                }
            }
        }

        return result;
    }

    // account data ////////////////////////////////////////////////////////////
    public static bool IsValidAccount(string account, string password)
    {
        // this function can be used to verify account credentials in a database
        // or a content management system.
        //
        // for example, we could setup a content management system with a forum,
        // news, shop etc. and then use a simple HTTP-GET to check the account
        // info, for example:
        //
        //   var request = new WWW("example.com/verify.php?id="+id+"&amp;pw="+pw);
        //   while (!request.isDone)
        //       print("loading...");
        //   return request.error == null && request.text == "ok";
        //
        // where verify.php is a script like this one:
        //   <?php
        //   // id and pw set with HTTP-GET?
        //   if (isset($_GET['id']) && isset($_GET['pw']))
        //   {
        //       // validate id and pw by using the CMS, for example in Drupal:
        //       if (user_authenticate($_GET['id'], $_GET['pw']))
        //           echo "ok";
        //       else
        //           echo "invalid id or pw";
        //   }
        //   ?>
        //
        // or we could check in a MYSQL database:
        //   var dbConn = new MySql.Data.MySqlClient.MySqlConnection("Persist Security Info=False;server=localhost;database=notas;uid=root;password=" + dbpwd);
        //   var cmd = dbConn.CreateCommand();
        //   cmd.CommandText = "SELECT id FROM accounts WHERE id='" + account + "' AND pw='" + password + "'";
        //   dbConn.Open();
        //   var reader = cmd.ExecuteReader();
        //   if (reader.Read())
        //       return reader.ToString() == account;
        //   return false;
        //
        // as usual, we will use the simplest solution possible:
        // create account if not exists, compare password otherwise.
        // no CMS communication necessary and good enough for an Indie MMORPG.

        // not empty?
        if (!string.IsNullOrWhiteSpace(account) && !string.IsNullOrWhiteSpace(password))
        {
            List< List<object> > table = ExecuteReader("SELECT password, banned FROM accounts WHERE name=@name", new SqliteParameter("@name", account));
            if (table.Count == 1)
            {
                // account exists. check password and ban status.
                List<object> row = table[0];
                return (string)row[0] == password && (long)row[1] == 0;
            }
            else
            {
                // account doesn't exist. create it.
                ExecuteNonQuery("INSERT INTO accounts VALUES (@name, @password, 0)", new SqliteParameter("@name", account), new SqliteParameter("@password", password));
                return true;
            }
        }
        return false;
    }

    // character data //////////////////////////////////////////////////////////
    public static bool CharacterExists(string characterName)
    {
        // checks deleted ones too so we don't end up with duplicates if we un-
        // delete one
        return ((long)ExecuteScalar("SELECT Count(*) FROM characters WHERE name=@name", new SqliteParameter("@name", characterName))) == 1;
    }

    public static void CharacterDelete(string characterName)
    {
        // soft delete the character so it can always be restored later
        ExecuteNonQuery("UPDATE characters SET deleted=1 WHERE name=@character", new SqliteParameter("@character", characterName));
    }

    // returns the list of character names for that account
    // => all the other values can be read with CharacterLoad!
    public static List<string> CharactersForAccount(string account)
    {
        List<string> result = new List<string>();
        List< List<object> > table = ExecuteReader("SELECT name FROM characters WHERE account=@account AND deleted=0", new SqliteParameter("@account", account));
        foreach (List<object> row in table)
            result.Add((string)row[0]);
        return result;
    }

    static void LoadInventory(PlayerInventory inventory)
    {
        // fill all slots first
        for (int i = 0; i < inventory.size; ++i)
            inventory.slots.Add(new ItemSlot());

        // then load valid items and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        List< List<object> > table = ExecuteReader("SELECT name, slot, amount, ammo, durability FROM character_inventory WHERE character=@character", new SqliteParameter("@character", inventory.name));
        foreach (List<object> row in table)
        {
            string itemName = (string)row[0];
            int slot = Convert.ToInt32((long)row[1]);
            if (slot < inventory.size)
            {
                ScriptableItem itemData;
                if (ScriptableItem.dict.TryGetValue(itemName.GetStableHashCode(), out itemData))
                {
                    Item item = new Item(itemData);
                    int amount = Convert.ToInt32((long)row[2]);
                    item.ammo = Convert.ToInt32((long)row[3]);
                    item.durability = Mathf.Min(Convert.ToInt32((long)row[4]), item.maxDurability);
                    inventory.slots[slot] = new ItemSlot(item, amount);
                }
                else Debug.LogWarning("LoadInventory: skipped item " + itemName + " for " + inventory.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
            }
            else Debug.LogWarning("LoadInventory: skipped slot " + slot + " for " + inventory.name + " because it's bigger than size " + inventory.size);
        }
    }

    static void LoadEquipment(PlayerEquipment equipment)
    {
        // fill all slots first
        for (int i = 0; i < equipment.slotInfo.Length; ++i)
            equipment.slots.Add(new ItemSlot());

        // then load valid equipment and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        List< List<object> > table = ExecuteReader("SELECT name, slot, amount, ammo, durability FROM character_equipment WHERE character=@character", new SqliteParameter("@character", equipment.name));
        foreach (List<object> row in table)
        {
            string itemName = (string)row[0];
            int slot = Convert.ToInt32((long)row[1]);
            if (slot < equipment.slotInfo.Length)
            {
                ScriptableItem itemData;
                if (ScriptableItem.dict.TryGetValue(itemName.GetStableHashCode(), out itemData))
                {
                    Item item = new Item(itemData);
                    int amount = Convert.ToInt32((long)row[2]);
                    item.ammo = Convert.ToInt32((long)row[3]);
                    item.durability = Mathf.Min(Convert.ToInt32((long)row[4]), item.maxDurability);
                    equipment.slots[slot] = new ItemSlot(item, amount);
                }
                else Debug.LogWarning("LoadEquipment: skipped item " + itemName + " for " + equipment.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
            }
            else Debug.LogWarning("LoadEquipment: skipped slot " + slot + " for " + equipment.name + " because it's bigger than size " + equipment.slotInfo.Length);
        }
    }

    static void LoadHotbar(PlayerHotbar hotbar)
    {
        // fill all slots first
        for (int i = 0; i < hotbar.size; ++i)
            hotbar.slots.Add(new ItemSlot());

        // then load valid items and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        List< List<object> > table = ExecuteReader("SELECT name, slot, amount, ammo, durability FROM character_hotbar WHERE character=@character", new SqliteParameter("@character", hotbar.name));
        foreach (List<object> row in table)
        {
            string itemName = (string)row[0];
            int slot = Convert.ToInt32((long)row[1]);
            if (slot < hotbar.size)
            {
                ScriptableItem itemData;
                if (ScriptableItem.dict.TryGetValue(itemName.GetStableHashCode(), out itemData))
                {
                    Item item = new Item(itemData);
                    int amount = Convert.ToInt32((long)row[2]);
                    item.ammo = Convert.ToInt32((long)row[3]);
                    item.durability = Mathf.Min(Convert.ToInt32((long)row[4]), item.maxDurability);
                    hotbar.slots[slot] = new ItemSlot(item, amount);
                }
                else Debug.LogWarning("LoadHotbar: skipped item " + itemName + " for " + hotbar.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
            }
            else Debug.LogWarning("LoadHotbar: skipped slot " + slot + " for " + hotbar.name + " because it's bigger than size " + hotbar.size);
        }
    }

    public static GameObject CharacterLoad(string characterName, List<GameObject> prefabs)
    {
        List< List<object> > table = ExecuteReader("SELECT * FROM characters WHERE name=@name AND deleted=0", new SqliteParameter("@name", characterName));
        if (table.Count == 1)
        {
            List<object> mainrow = table[0];

            // instantiate based on the class name
            string className = (string)mainrow[2];
            GameObject prefab = prefabs.Find(p => p.name == className);
            if (prefab != null)
            {
                GameObject player = GameObject.Instantiate(prefab.gameObject);

                player.name                                 = (string)mainrow[0];
                player.GetComponent<PlayerMeta>().account   = (string)mainrow[1];
                player.GetComponent<PlayerMeta>().className = (string)mainrow[2];
                float x                                     = (float)mainrow[3];
                float y                                     = (float)mainrow[4];
                float z                                     = (float)mainrow[5];
                player.transform.position                   = new Vector3(x, y, z);
                int health                                  = Convert.ToInt32((long)mainrow[6]);
                int hydration                               = Convert.ToInt32((long)mainrow[7]);
                int nutrition                               = Convert.ToInt32((long)mainrow[8]);
                int temperature                             = Convert.ToInt32((long)mainrow[9]);
                int endurance                               = Convert.ToInt32((long)mainrow[10]);

                LoadInventory(player.GetComponent<PlayerInventory>());
                LoadEquipment(player.GetComponent<PlayerEquipment>());
                LoadHotbar(player.GetComponent<PlayerHotbar>());

                // assign health / hydration etc. after max values were fully loaded
                // (they depend on equipment)
                player.GetComponent<Health>().current = health;
                player.GetComponent<Hydration>().current = hydration;
                player.GetComponent<Nutrition>().current = nutrition;
                player.GetComponent<Temperature>().current = temperature;
                player.GetComponent<Endurance>().current = endurance;

                return player;
            }
            else Debug.LogError("no prefab found for class: " + className);
        }
        return null;
    }

    static void SaveInventory(PlayerInventory inventory)
    {
        // inventory: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQuery("DELETE FROM character_inventory WHERE character=@character", new SqliteParameter("@character", inventory.name));
        for (int i = 0; i < inventory.slots.Count; ++i)
        {
            ItemSlot slot = inventory.slots[i];
            if (slot.amount > 0) // only relevant items to save queries/storage/time
                ExecuteNonQuery("INSERT INTO character_inventory VALUES (@character, @slot, @name, @amount, @ammo, @durability)",
                                new SqliteParameter("@character", inventory.name),
                                new SqliteParameter("@slot", i),
                                new SqliteParameter("@name", slot.item.name),
                                new SqliteParameter("@amount", slot.amount),
                                new SqliteParameter("@ammo", slot.item.ammo),
                                new SqliteParameter("@durability", slot.item.durability));
        }
    }

    static void SaveEquipment(PlayerEquipment equipment)
    {
        // equipment: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQuery("DELETE FROM character_equipment WHERE character=@character", new SqliteParameter("@character", equipment.name));
        for (int i = 0; i < equipment.slots.Count; ++i)
        {
            ItemSlot slot = equipment.slots[i];
            if (slot.amount > 0) // only relevant equip to save queries/storage/time
                ExecuteNonQuery("INSERT INTO character_equipment VALUES (@character, @slot, @name, @amount, @ammo, @durability)",
                                new SqliteParameter("@character", equipment.name),
                                new SqliteParameter("@slot", i),
                                new SqliteParameter("@name", slot.item.name),
                                new SqliteParameter("@amount", slot.amount),
                                new SqliteParameter("@ammo", slot.item.ammo),
                                new SqliteParameter("@durability", slot.item.durability));
        }
    }

    static void SaveHotbar(PlayerHotbar hotbar)
    {
        // hotbar: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQuery("DELETE FROM character_hotbar WHERE character=@character", new SqliteParameter("@character", hotbar.name));
        for (int i = 0; i < hotbar.slots.Count; ++i)
        {
            ItemSlot slot = hotbar.slots[i];
            if (slot.amount > 0) // only relevant items to save queries/storage/time
                ExecuteNonQuery("INSERT INTO character_hotbar VALUES (@character, @slot, @name, @amount, @ammo, @durability)",
                                new SqliteParameter("@character", hotbar.name),
                                new SqliteParameter("@slot", i),
                                new SqliteParameter("@name", slot.item.name),
                                new SqliteParameter("@amount", slot.amount),
                                new SqliteParameter("@ammo", slot.item.ammo),
                                new SqliteParameter("@durability", slot.item.durability));
        }
    }

    // adds or overwrites character data in the database
    public static void CharacterSave(GameObject player, bool online, bool useTransaction = true)
    {
        // only use a transaction if not called within SaveMany transaction
        if (useTransaction) ExecuteNonQuery("BEGIN");

        // online status:
        //   '' if offline (if just logging out etc.)
        //   current time otherwise
        // -> this way it's fault tolerant because external applications can
        //    check if online != '' and if time difference < saveinterval
        // -> online time is useful for network zones (server<->server online
        //    checks), external websites which render dynamic maps, etc.
        // -> it uses the ISO 8601 standard format
        string onlineString = online ? DateTime.UtcNow.ToString("s") : "";

        ExecuteNonQuery("INSERT OR REPLACE INTO characters VALUES (@name, @account, @class, @x, @y, @z, @health, @hydration, @nutrition, @temperature, @endurance, @online, 0)",
                        new SqliteParameter("@name", player.name),
                        new SqliteParameter("@account", player.GetComponent<PlayerMeta>().account),
                        new SqliteParameter("@class", player.GetComponent<PlayerMeta>().className),
                        new SqliteParameter("@x", player.transform.position.x),
                        new SqliteParameter("@y", player.transform.position.y),
                        new SqliteParameter("@z", player.transform.position.z),
                        new SqliteParameter("@health", player.GetComponent<Health>().current),
                        new SqliteParameter("@hydration", player.GetComponent<Hydration>().current),
                        new SqliteParameter("@nutrition", player.GetComponent<Nutrition>().current),
                        new SqliteParameter("@temperature", player.GetComponent<Temperature>().current),
                        new SqliteParameter("@endurance", player.GetComponent<Endurance>().current),
                        new SqliteParameter("@online", onlineString));

        SaveInventory(player.GetComponent<PlayerInventory>());
        SaveEquipment(player.GetComponent<PlayerEquipment>());
        SaveHotbar(player.GetComponent<PlayerHotbar>());

        if (useTransaction) ExecuteNonQuery("END");
    }

    // save multiple characters at once (useful for ultra fast transactions)
    public static void CharacterSaveMany(List<GameObject> players, bool online = true)
    {
        ExecuteNonQuery("BEGIN"); // transaction for performance
        foreach (GameObject player in players) CharacterSave(player, online, false);
        ExecuteNonQuery("END");
    }

    // storage /////////////////////////////////////////////////////////////////
    public static void LoadStorage(Storage storage)
    {
        // fill all slots first
        for (int i = 0; i < storage.size; ++i)
            storage.slots.Add(new ItemSlot());

        // then load valid items and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        List< List<object> > table = ExecuteReader("SELECT name, slot, amount, ammo, durability FROM storages WHERE storage=@storage", new SqliteParameter("@storage", storage.name));
        foreach (List<object> row in table)
        {
            string itemName = (string)row[0];
            int slot = Convert.ToInt32((long)row[1]);
            ScriptableItem itemData;
            if (slot < storage.size && ScriptableItem.dict.TryGetValue(itemName.GetStableHashCode(), out itemData))
            {
                Item item = new Item(itemData);
                int amount = Convert.ToInt32((long)row[2]);
                item.ammo = Convert.ToInt32((long)row[3]);
                item.durability = Mathf.Min(Convert.ToInt32((long)row[4]), item.maxDurability);
                storage.slots[slot] = new ItemSlot(item, amount);
            }
        }
    }

    static void SaveStorage(Storage storage, bool useTransaction = true)
    {
        // only use a transaction if not called within SaveMany transaction
        if (useTransaction) ExecuteNonQuery("BEGIN");

        // storage: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQuery("DELETE FROM storages WHERE storage=@storage", new SqliteParameter("@storage", storage.name));
        for (int i = 0; i < storage.slots.Count; ++i)
        {
            ItemSlot slot = storage.slots[i];
            if (slot.amount > 0) // only relevant items to save queries/storage/time
                ExecuteNonQuery("INSERT INTO storages VALUES (@storage, @slot, @name, @amount, @ammo, @durability)",
                                new SqliteParameter("@storage", storage.name),
                                new SqliteParameter("@slot", i),
                                new SqliteParameter("@name", slot.item.name),
                                new SqliteParameter("@amount", slot.amount),
                                new SqliteParameter("@ammo", slot.item.ammo),
                                new SqliteParameter("@durability", slot.item.durability));
        }

        if (useTransaction) ExecuteNonQuery("END");
    }

    // save multiple storages at once (useful for ultra fast transactions)
    public static void SaveStorages(List<Storage> storages)
    {
        ExecuteNonQuery("BEGIN"); // transaction for performance
        foreach (Storage storage in storages) SaveStorage(storage, false);
        ExecuteNonQuery("END");
    }

    // structures //////////////////////////////////////////////////////////////
    static void SaveStructure(GameObject structure, bool useTransaction = true)
    {
        // only use a transaction if not called within SaveMany transaction
        if (useTransaction) ExecuteNonQuery("BEGIN");

        // get position and rotation (so we don't have to access .transform 6x)
        Vector3 position = structure.transform.position;
        Vector3 rotation = structure.transform.rotation.eulerAngles;

        ExecuteNonQuery("INSERT INTO structures VALUES (@name, @x, @y, @z, @xRot, @yRot, @zRot)",
                        new SqliteParameter("@name", structure.name),
                        new SqliteParameter("@x", position.x),
                        new SqliteParameter("@y", position.y),
                        new SqliteParameter("@z", position.z),
                        new SqliteParameter("@xRot", rotation.x),
                        new SqliteParameter("@yRot", rotation.y),
                        new SqliteParameter("@zRot", rotation.z));

        if (useTransaction) ExecuteNonQuery("END");
    }

    public static void SaveStructures(List<GameObject> structures)
    {
        ExecuteNonQuery("BEGIN"); // transaction for performance

        // remove all old entries first, then add all the new ones
        // (we could use UPDATE where ... but deleting everything makes sure
        //  that there are never any ghosts)
        ExecuteNonQuery("DELETE FROM structures");
        foreach (GameObject structure in structures) SaveStructure(structure, false);

        ExecuteNonQuery("END");
    }

    // loads and spawns all structures
    public static void LoadStructures()
    {
        // build a dict of spawnable structures so we don't have to go through
        // the networkmanager's spawnable prefabs for each one of them
        Dictionary<string, GameObject> spawnable = NetworkManager.singleton.spawnPrefabs
                                                     .Where(p => p.tag == "Structure")
                                                     .ToDictionary(p => p.name, p => p);

        List< List<object> > table = ExecuteReader("SELECT * FROM structures");
        foreach (List<object> row in table)
        {
            string structureName = (string)row[0];
            float x = (float)row[1];
            float y = (float)row[2];
            float z = (float)row[3];
            float xRot = (float)row[4];
            float yRot = (float)row[5];
            float zRot = (float)row[6];

            // do we still have a spawnable structure with that name?
            if (spawnable.ContainsKey(structureName))
            {
                Vector3 position = new Vector3(x, y, z);
                Quaternion rotation = Quaternion.Euler(xRot, yRot, zRot);
                GameObject prefab = spawnable[structureName];
                GameObject go = GameObject.Instantiate(prefab, position, rotation);
                go.name = prefab.name; // avoid "(Clone)". important for saving.
                NetworkServer.Spawn(go);
            }
        }
    }
}