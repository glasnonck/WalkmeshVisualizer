using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
        public override string ToString() => $"({x},{y},{z})";
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
        public uint OFFSET_CSWSOBJECT_Z_POS;
        public uint OFFSET_CSWSOBJECT_X_DIR;
        public uint OFFSET_CSWSOBJECT_Y_DIR;
        public uint OFFSET_CSWSOBJECT_Z_DIR;
        public uint LEADER_BEARING;
        public uint LEADER_X_POS;
        public uint LEADER_Y_POS;
        public uint LEADER_Z_POS;
        public uint[] CAMERA_ANGLE;
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

        public uint[] AREA_NAME;
        public uint[] LOAD_DIRECTION;

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
                OFFSET_CSWSOBJECT_Z_POS = 0x98;
                OFFSET_CSWSOBJECT_X_DIR = 0x9c;
                OFFSET_CSWSOBJECT_Y_DIR = 0xa0;
                OFFSET_CSWSOBJECT_Z_DIR = 0xa4;
                OFFSET_CSWSTRIGGER_GEOMETRY_COUNT = 0x284;
                OFFSET_CSWSTRIGGER_GEOMETRY = 0x288;

                AREA_NAME = new uint[] { 0x007A39E8, 0x4C, 0x0 };
                LEADER_BEARING = 0x00833924;
                LEADER_X_POS = 0x00833928;
                LEADER_Y_POS = 0x0083392c;
                LEADER_Z_POS = 0x00833930;
                CAMERA_ANGLE = new uint[] { 0x00833bb4, 0x20, 0x184, 0x58c };
                LOAD_DIRECTION = new uint[] { 0x007a39fc, 0x4, 0x4, 0x278, 0xc8 };
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
                OFFSET_CSWSOBJECT_Z_POS = 0x9c;
                OFFSET_CSWSOBJECT_X_DIR = 0xa0;
                OFFSET_CSWSOBJECT_Y_DIR = 0xa4;
                OFFSET_CSWSOBJECT_Z_DIR = 0xa8;
                OFFSET_CSWSTRIGGER_GEOMETRY_COUNT = 0x2c4;
                OFFSET_CSWSTRIGGER_GEOMETRY = 0x2c8;

                AREA_NAME = new uint[] { 0x00a1B4A4, 0x4, 0x4, 0x2FC, 0x5 };
                LEADER_BEARING = 0x00a13458;
                LEADER_X_POS = 0x00a1345c;
                LEADER_Y_POS = 0x00a13460;
                LEADER_Z_POS = 0x00a13464;
                CAMERA_ANGLE = new uint[] { };
                LOAD_DIRECTION = new uint[] { 0x00a11c04, 0x4, 0x4, 0x278, 0xd0 };
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

        public void UseK2SteamAddress()
        {
            ADDRESS_APP_MANAGER = 0x00a1b4a4;
            LEADER_BEARING = 0x00a7fe80;
            LEADER_X_POS = 0x00a7fe84;
            LEADER_Y_POS = 0x00a7fe88;
            LEADER_Z_POS = 0x00a7fe8c;
            LOAD_DIRECTION = new uint[] { 0x00a1b4a4, 0x4, 0x4, 0x278, 0xd0 };
        }

        public static uint GetGOAIndexFromServerID(uint id)
            => (uint)(((int)id >> 0x1f) * -0x1000 + (id & 0xfff));

        public static uint ClientToServerID(uint client_id)
            => client_id & 0x7fffffff;

        public static uint ServerToClientID(uint server_id)
            => server_id | 0x80000000;
    }

    public class KotorManager
    {
        const int K2_STEAM_MODULE_SIZE = 7049216;
        const int K2_GOG_MODULE_SIZE = 7012352;

        public ProcessReader pr { get; set; }
        public KotorAddresses ka { get; set; }

        uint app_manager;
        uint client_internal;
        uint server_internal;
        uint server_game_object_array;
        readonly int version;

        public KotorManager(int version = 1)
        {
            pr = null;
            this.version = version;
            RefreshAddresses();
        }

        public void RefreshAddresses()
        {
            ka = new KotorAddresses(version);
            pr = new ProcessReader(ka.KOTOR_EXE);

            if (version == 2 && pr.GetModuleSize() == K2_STEAM_MODULE_SIZE)
                ka.UseK2SteamAddress();

            // get app manager
            pr.ReadUint(ka.ADDRESS_APP_MANAGER, out app_manager);
            
            // get client internal
            pr.ReadUint(app_manager + ka.OFFSET_CLIENT, out uint temp);
            pr.ReadUint(temp + ka.OFFSET_INTERNAL, out client_internal);
            
            // get server_internal
            pr.ReadUint(app_manager + ka.OFFSET_SERVER, out temp);
            pr.ReadUint(temp + ka.OFFSET_INTERNAL, out server_internal);
            
            // get server game object array
            pr.ReadUint(server_internal + ka.OFFSET_SERVER_GAME_OBJ_ARR, out temp);
            pr.ReadUint(temp, out server_game_object_array);
        }

        public uint GetClientPlayerID()
        {
            if (pr.hasFailed)
            {
                RefreshAddresses();
                pr.hasFailed = false;
            }

            pr.ReadUint(client_internal + ka.OFFSET_CLIENT_PLAYER_ID, out uint client_player_id);
            return client_player_id;
        }

        public uint GetPlayerGameObject()
        {
            if (pr.hasFailed)
            {
                RefreshAddresses();
                pr.hasFailed = false;
            }

            return GetGameObjectByClientID(GetClientPlayerID());
        }

        private uint GetGameObjectByClientID(uint client_id) => GetGameObjectByServerID(KotorAddresses.ClientToServerID(client_id));

        private uint GetGameObjectByServerID(uint server_id)
        {
            if (pr.hasFailed)
            {
                RefreshAddresses();
                pr.hasFailed = false;
            }

            uint goa_index = KotorAddresses.GetGOAIndexFromServerID(server_id);
            uint goa_offset = goa_index * sizeof(uint);
            pr.ReadUint(server_game_object_array + goa_offset, out var goa_ptr);
            //pr.ReadGameObjectEntry(goa_ptr, out var goa);
            //return goa.game_object_pointer;
            pr.ReadUint(goa_ptr + sizeof(uint), out uint gop);
            return gop;
        }

        public Point GetLeaderPosition()
        {
            var pgo = GetPlayerGameObject();
            pr.ReadFloat(pgo + ka.OFFSET_CSWSOBJECT_X_POS, out float x);
            pr.ReadFloat(pgo + ka.OFFSET_CSWSOBJECT_Y_POS, out float y);
            //pr.ReadFloat(ka.LEADER_X_POS, out float x);
            //pr.ReadFloat(ka.LEADER_Y_POS, out float y);
            return new Point(x, y);
        }

        public float GetLeaderBearing()
        {
            var pgo = GetPlayerGameObject();
            pr.ReadFloat(pgo + ka.OFFSET_CSWSOBJECT_X_DIR, out float x);
            pr.ReadFloat(pgo + ka.OFFSET_CSWSOBJECT_Y_DIR, out float y);
            if (x == 0) return y >= 0 ?  0f : 180f;
            if (y == 0) return x >= 0 ? 90f : -90f;
            return (float)(180 / Math.PI * Math.Atan2(y, x)) - 90f;
        }

        public float GetPlayerBearing()
        {
            pr.ReadFloat(ka.LEADER_BEARING, out float b);
            return b;
        }

        public float GetCameraAngle()
        {
            if (version == 2) { pr.hasFailed = true; return 0; }
            pr.GetFinalPointerAddress(ka.CAMERA_ANGLE, out uint address);
            if (pr.hasFailed) return 0;
            pr.ReadFloat(address, out float output);
            if (pr.hasFailed) return 0;
            return output;
        }

        public List<Tuple<string,List<GameVector>>> GetDoorCorners()
        {
            var output = new List<Tuple<string,List<GameVector>>>();

            List<uint> areaObjects = GetAllObjectsInArea();
            List<uint> areaDoors = new List<uint>();
            foreach (var objPtr in areaObjects)
            {
                var obj = GetGameObjectByServerID(objPtr);
                pr.ReadByte(obj + ka.OFFSET_GAME_OBJECT_TYPE, out byte type);
                if ((GameObjectTypes)type == GameObjectTypes.Door)
                {
                    pr.ReadByte(obj + ka.OFFSET_CSWSDOOR_LINKED_TO_FLAGS, out byte flags);
                    if (flags != 0) areaDoors.Add(obj);
                }
            }

            foreach (var doorPtr in areaDoors)
            {
                pr.ReadUint(doorPtr + ka.OFFSET_CSWSDOOR_LINKED_TO_MODULE + ka.OFFSET_CEXOSTRING_LENGTH, out uint length);
                pr.ReadUint(doorPtr + ka.OFFSET_CSWSDOOR_LINKED_TO_MODULE, out uint strPtr);
                pr.ReadString(strPtr, out string module, length);
                var null_index = module.IndexOf('\0');
                if (null_index >= 0) module = module.Substring(0, null_index);

                var corners = new List<GameVector>();
                pr.ReadVector(doorPtr + ka.OFFSET_CSWSDOOR_CORNERS, out GameVector c1);
                corners.Add(c1);
                pr.ReadVector(doorPtr + ka.OFFSET_CSWSDOOR_CORNERS + 12, out GameVector c2);
                corners.Add(c2);
                pr.ReadVector(doorPtr + ka.OFFSET_CSWSDOOR_CORNERS + 24, out GameVector c3);
                corners.Add(c3);
                pr.ReadVector(doorPtr + ka.OFFSET_CSWSDOOR_CORNERS + 36, out GameVector c4);
                corners.Add(c4);
                output.Add(new Tuple<string,List<GameVector>>(module, corners));
            }

            return output;
        }

        private List<uint> GetAllObjectsInArea()
        {
            var agop = GetAreaGameObject();
            if (agop == 0x0) return null;

            var output = new List<uint>();

            pr.ReadUint(agop + ka.OFFSET_AREA_GAME_OBJECT_ARRAY, out uint arrayPointer);
            pr.ReadUint(agop + ka.OFFSET_AREA_GAME_OBJECT_ARRAY_LENGTH, out uint arrayLength);

            uint size = sizeof(uint);
            for (uint i = 0; i < arrayLength; i++)
            {
                pr.ReadUint(arrayPointer + (size * i), out uint temp);
                output.Add(temp);
            }

            return output;
        }

        private uint GetAreaGameObject()
        {
            var pgo = GetPlayerGameObject();
            pr.ReadUint(pgo + ka.OFFSET_CSWSOBJECT_AREA_ID, out uint areaId);
            var areaGameObjectPointer = GetGameObjectByServerID(areaId);
            pr.ReadByte(areaGameObjectPointer + ka.OFFSET_GAME_OBJECT_TYPE, out byte temp);
            var areaGameObjectType = (GameObjectTypes)temp;
            if (areaGameObjectType != GameObjectTypes.Area)
            {
                Console.WriteLine("Not In Module...");
                pr.hasFailed = true;
                return 0x0;
            }
            return areaGameObjectPointer;
        }

        public bool SetLoadDirection()
        {
            //if (version == 2) { pr.hasFailed = true; return 0; }
            pr.GetFinalPointerAddress(ka.LOAD_DIRECTION, out uint address);
            if (pr.hasFailed) return false;
            pr.WriteUint(address, 0);
            if (pr.hasFailed) return false;
            return true;
        }

        public bool TestRead()
        {
            var readSuccess = pr.ReadInt(ka.ADDRESS_BASE, out int testRead);
            if (!readSuccess) Console.WriteLine($"Failed Test Read!\r\nExpected: {KotorAddresses.TEST_READ_VALUE}\r\nGot: {testRead}");
            return readSuccess && KotorAddresses.TEST_READ_VALUE == testRead;
        }
    }

    public class ProcessReader
    {
        const int PROCESS_WM_READ = 0x0010;

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }

        #region Write Signatures
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hProcess);
        #endregion

        #region Read Signatures
        //[DllImport("kernel32.dll")]
        //public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        #endregion

        public Process p;
        public IntPtr h;
        public bool hasFailed = false;

        public ProcessReader(string processName)
        {
            p = Process.GetProcessesByName(processName).FirstOrDefault();
            if (p == null) throw new NullReferenceException($"Process {processName} does not exist.");
            h = OpenProcess(ProcessAccessFlags.All, false, p.Id);
            //h = OpenProcess(PROCESS_WM_READ, false, p.Id);
        }

        public int GetModuleSize() => p.MainModule.ModuleMemorySize;

        public bool ReadInt(uint address, out int output)
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

        public bool ReadByte(uint address, out byte output)
        {
            output = 0;
            var buffer = new byte[sizeof(byte)];
            int bytesRead = 0;

            if (ReadProcessMemory(h, new IntPtr(address), buffer, buffer.Length, ref bytesRead))
            {
                output = buffer[0];
                return true;
            }

            hasFailed = true;
            return false;
        }

        public bool ReadUint(uint address, out uint output)
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

        public bool WriteUint(uint address, uint input)
        {
            var buffer = new byte[] { (byte)input };
            if (WriteProcessMemory(h, new IntPtr(address), buffer, (uint)buffer.Length, out int wtf))
                return true;

            hasFailed = true;
            return false;
        }

        public bool ReadString(uint address, out string output, uint length)
        {
            output = null;
            var buffer = new byte[length];
            int bytesRead = 0;

            if (ReadProcessMemory(h, new IntPtr(address), buffer, buffer.Length, ref bytesRead))
            {
                //output = BitConverter.ToString(buffer, 0);
                output = Encoding.Default.GetString(buffer, 0, bytesRead);
                return true;
            }

            hasFailed = true;
            return false;
        }

        public bool ReadFloat(uint address, out float output)
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

        internal bool ReadVector(uint address, out GameVector output)
        {
            output = new GameVector();
            hasFailed = !ReadFloat(address, out float x);
            if (hasFailed) return false;

            hasFailed = !ReadFloat(address + 4, out float y);
            if (hasFailed) return false;

            hasFailed = !ReadFloat(address + 8, out float z);
            if (hasFailed) return false;

            output.x = x;
            output.y = y;
            output.z = z;
            return true;
        }

        public bool ReadGameObjectEntry(uint address, out GameObjectEntry output)
        {
            output = new GameObjectEntry();
            hasFailed = !ReadUint(address, out uint id);
            if (hasFailed) return false;

            hasFailed = !ReadUint(address + sizeof(uint), out uint gop);
            if (hasFailed) return false;

            hasFailed = !ReadUint(address + (2 * sizeof(uint)), out uint next);
            if (hasFailed) return false;

            output.id = id;
            output.game_object_pointer = gop;
            output.next = next;
            return true;
        }

        public bool GetFinalPointerAddress(uint[] addresses, out uint output)
        {
            output = 0;
            uint idx = 0;

            for (int i = 0; i < addresses.Length - 1; i++)
            {
                hasFailed = !ReadUint(addresses[i] + idx, out idx);
                if (hasFailed) return false;
            }

            output = addresses.Last() + idx;
            return !hasFailed;
        }

        public bool ReadAreaName(int version, uint[] addresses, out string output)
        {
            output = null;
            uint idx = 0;
            for (int i = 0; i < addresses.Length - 1; i++)
            {
                hasFailed = !ReadUint(addresses[i] + idx, out idx);
                if (hasFailed) return false;
            }

            var length = version == 1 ? 10 : 6;
            return ReadString(addresses.Last() + idx, out output, (uint)length);
        }
    }
}
