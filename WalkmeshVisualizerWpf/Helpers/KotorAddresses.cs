using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WalkmeshVisualizerWpf.Helpers
{
    public struct GameObjectEntry
    {
        public uint id;
        public uint game_object_pointer;
        public uint next;
    }

    public struct GameVector
    {
        public float x;
        public float y;
        public float z;
    }

    public enum GameObjectTypes : byte
    {
        Area = 4,
        Creature,
        Item,
        Trigger,
        Projectile,
        Placeable,
        Door,
        AreaOfEffect,
        Waypoint,
        Encounter,
        Store = 14,
        Sound = 16,

    }

    public class KotorAddresses
    {
        public const string KOTOR_1_EXE = "swkotor";
        public const string KOTOR_2_EXE = "swkotor2";
        public const int TEST_READ_VALUE = 0x00905a4d;

        public string KOTOR_EXE;
        public uint ADDRESS_APP_MANAGER;
        public uint OFFSET_CSWSOBJECT_AREA_ID;
        public uint OFFSET_CSWSDOOR_CORNERS;
        public uint OFFSET_CSWSDOOR_LINKED_TO_FLAGS;
        public uint OFFSET_CSWSDOOR_LINKED_TO_MODULE;
        public uint OFFSET_CSWSOBJECT_X_POS;
        public uint OFFSET_CSWSOBJECT_Y_POS;
        public uint OFFSET_CSWSTRIGGER_GEOMETRY_COUNT;
        public uint OFFSET_CSWSTRIGGER_GEOMETRY;

        public uint ADDRESS_BASE;

        public uint OFFSET_CLIENT;
        public uint OFFSET_SERVER;
        public uint OFFSET_INTERNAL;
        public uint OFFSET_SERVER_GAME_OBJ_ARR;
        public uint OFFSET_CLIENT_PLAYER_ID;
        public uint OFFSET_AREA_GAME_OBJECT_ARRAY;
        public uint OFFSET_AREA_GAME_OBJECT_ARRAY_LENGTH;
        public uint OFFSET_GAME_OBJECT_TYPE;
        public uint OFFSET_CEXOSTRING_LENGTH;
        public uint OFFSET_CSWSOBJECT_TAG;

        public KotorAddresses(int version = 1)
        {
            if (version == 1)
            {
                KOTOR_EXE = KOTOR_1_EXE;
                ADDRESS_APP_MANAGER = 0x007a39fc;
                OFFSET_CSWSOBJECT_AREA_ID = 0x8c;
                OFFSET_CSWSDOOR_CORNERS = 0x350;
                OFFSET_CSWSDOOR_LINKED_TO_FLAGS = 0x384;
                OFFSET_CSWSDOOR_LINKED_TO_MODULE = 0x390;
                OFFSET_CSWSOBJECT_X_POS = 0x90;
                OFFSET_CSWSOBJECT_Y_POS = 0x94;
                OFFSET_CSWSTRIGGER_GEOMETRY_COUNT = 0x284;
                OFFSET_CSWSTRIGGER_GEOMETRY = 0x288;
            }
            else if (version == 2)
            {
                KOTOR_EXE = KOTOR_2_EXE;
                ADDRESS_APP_MANAGER = 0x00a11c04;
                OFFSET_CSWSOBJECT_AREA_ID = 0x90;
                OFFSET_CSWSDOOR_CORNERS = 0x3a0;
                OFFSET_CSWSDOOR_LINKED_TO_FLAGS = 0x3d4;
                OFFSET_CSWSDOOR_LINKED_TO_MODULE = 0x3e0;
                OFFSET_CSWSOBJECT_X_POS = 0x94;
                OFFSET_CSWSOBJECT_Y_POS = 0x98;
                OFFSET_CSWSTRIGGER_GEOMETRY_COUNT = 0x2c4;
                OFFSET_CSWSTRIGGER_GEOMETRY = 0x2c8;
            }
            else
            {
                throw new Exception($"INVALID GAME VERSION {version}!");
            }

            ADDRESS_BASE = 0x00400000;
            OFFSET_CLIENT = 0x4;
            OFFSET_SERVER = 0x8;
            OFFSET_INTERNAL = 0x4;
            OFFSET_SERVER_GAME_OBJ_ARR = 0x1005c;
            OFFSET_CLIENT_PLAYER_ID = 0x20;
            OFFSET_AREA_GAME_OBJECT_ARRAY = 0x74;
            OFFSET_AREA_GAME_OBJECT_ARRAY_LENGTH = 0x78;
            OFFSET_GAME_OBJECT_TYPE = 0x8;
            OFFSET_CEXOSTRING_LENGTH = 0x4;
            OFFSET_CSWSOBJECT_TAG = 0x18;
        }

        public void swapK2SteamAddress()
            => ADDRESS_APP_MANAGER = 0x00a1b4a4;

        public static uint getGOAIndexFromServerID(uint id)
            => (uint)(((int)id >> 0x1f) * -0x1000 + (id & 0xfff));

        public static uint clientToServerID(uint client_id)
            => client_id & 0x7fffffff;

        public static uint serverToClientID(uint server_id)
            => server_id | 0x80000000;
    }

    public class KotorManager
    {
        const int K2_STEAM_MODULE_SIZE = 7049216;
        const int K2_GOG_MODULE_SIZE = 7012352;

        public ProcessReader pr;
        uint app_manager;
        uint client_internal;
        uint server_internal;
        uint server_game_object_array;
        public KotorAddresses ka;
        int version;

        public KotorManager(int version = 1)
        {
            pr = null;
            this.version = version;
            refreshAddresses();
        }

        public void refreshAddresses()
        {
            ka = new KotorAddresses(version);
            pr = new ProcessReader(ka.KOTOR_EXE);

            if (version == 2 && pr.getModuleSize() == K2_STEAM_MODULE_SIZE)
                ka.swapK2SteamAddress();

            //pr.readInt(ka.ADDRESS_BASE, out int testRead);

            var read1 = pr.readUint(ka.ADDRESS_APP_MANAGER, out app_manager);
                   
            var read2 = pr.readUint((app_manager + ka.OFFSET_CLIENT), out uint temp);
            var read3 = pr.readUint((temp + ka.OFFSET_INTERNAL), out client_internal);
            
            var read4 = pr.readUint((app_manager + ka.OFFSET_SERVER), out temp);
            var read5 = pr.readUint((temp + ka.OFFSET_INTERNAL), out server_internal);
            
            var read6 = pr.readUint((server_internal + ka.OFFSET_SERVER_GAME_OBJ_ARR), out temp);
            var read7 = pr.readUint(temp, out server_game_object_array);

            getClientPlayerID();
        }

        public uint getClientPlayerID()
        {
            if (pr.hasFailed)
            {
                refreshAddresses();
                pr.hasFailed = false;
            }

            pr.readUint((client_internal + ka.OFFSET_CLIENT_PLAYER_ID), out uint client_player_id);
            return client_player_id;
        }

        public uint getPlayerGameObject() => getGameObjectByClientID(getClientPlayerID());

        private uint getGameObjectByClientID(uint client_id) => getGameObjectByServerID(KotorAddresses.clientToServerID(client_id));

        private uint getGameObjectByServerID(uint server_id)
        {
            uint goa_index = KotorAddresses.getGOAIndexFromServerID(server_id);
            uint goa_offset = goa_index * sizeof(uint);
            pr.readUint(server_game_object_array + goa_offset, out var goa_ptr);
            pr.readGameObjectEntry(goa_ptr, out var goa);
            return goa.game_object_pointer;
        }

        private uint getGOAIndexFromServerID(uint server_id)
        {
            throw new NotImplementedException();
        }

        public Point getPlayerPosition()
        {
            var pgo = getPlayerGameObject();
            pr.readFloat(pgo + ka.OFFSET_CSWSOBJECT_X_POS, out float x);
            pr.readFloat(pgo + ka.OFFSET_CSWSOBJECT_Y_POS, out float y);
            return new Point(x, y);
        }
    }

    public class ProcessReader
    {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        public Process p;
        public IntPtr h;
        public bool hasFailed = false;

        public ProcessReader(string processName)
        {
            p = Process.GetProcessesByName(processName).FirstOrDefault();
            if (p == null) throw new NullReferenceException($"Process {processName} does not exist.");
            h = OpenProcess(PROCESS_WM_READ, false, p.Id);
        }

        public int getModuleSize() => p.MainModule.ModuleMemorySize;

        public bool readInt(uint address, out int output)
        {
            output = 0;
            var buffer = new byte[sizeof(int)];
            int bytesRead = 0;

            if (ReadProcessMemory(h, new IntPtr(address), buffer, buffer.Length, ref bytesRead))
            {
                output = BitConverter.ToInt32(buffer, 0);
                return true;
            }

            hasFailed = true;
            return false;
        }

        public bool readUint(uint address, out uint output)
        {
            output = 0;
            var buffer = new byte[sizeof(uint)];
            int bytesRead = 0;

            if (ReadProcessMemory(h, new IntPtr(address), buffer, buffer.Length, ref bytesRead))
            {
                output = BitConverter.ToUInt32(buffer, 0);
                return true;
            }

            hasFailed = true;
            return false;
        }

        public bool readFloat(uint address, out float output)
        {
            output = 0;
            var buffer = new byte[sizeof(float)];
            int bytesRead = 0;

            if (ReadProcessMemory(h, new IntPtr(address), buffer, buffer.Length, ref bytesRead))
            {
                output = BitConverter.ToSingle(buffer, 0);
                return true;
            }

            hasFailed = true;
            return false;
        }

        public bool readGameObjectEntry(uint address, out GameObjectEntry output)
        {
            output = new GameObjectEntry();
            hasFailed = !readUint(address, out uint id);
            if (hasFailed) return false;

            hasFailed = !readUint(address + sizeof(uint), out uint gop);
            if (hasFailed) return false;

            hasFailed = !readUint(address + (2 * sizeof(uint)), out uint next);
            if (hasFailed) return false;

            output.id = id;
            output.game_object_pointer = gop;
            output.next = next;
            return true;
        }
    }
}
