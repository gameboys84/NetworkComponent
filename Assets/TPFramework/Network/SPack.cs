using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TPFramework
{
    public class SPack {
    	const int HEADER_SIZE = 2;
		private int[] TypeBytes = {0,1,2,4,8,0}; //0,i8,i16,i32,i64,0

        public byte[] buf;
        public int offset;
        
        
    	public void SetHead() {
    		UInt16 size = (UInt16)(offset - HEADER_SIZE);
    		offset = 0;
    		Write(size);

    		Array.Resize(ref buf, size + HEADER_SIZE);
    	}

        public bool IsTail() {
            return offset >= buf.Length;
        }

        public int GetPos() {
            return offset;
        }

    	// read
        public Byte[] ReadBytes2() {
            UInt16 size = ReadUInt16();
            byte[] writeBuf = new byte[size];
            Array.Copy(buf, offset, writeBuf, 0, size);
            offset += size;
            return writeBuf;
        }
        public Byte[] ReadBytes4()
        {
            UInt32 size = ReadUInt32();
            byte[] writeBuf = new byte[size];
            Array.Copy(buf, offset, writeBuf, 0, size);
            offset += (int)size;
            return writeBuf;
        }

        public Byte ReadByte() {
            Byte val = buf[offset];
            offset += 1;
            return val;
        }

        public SByte ReadSByte() {
            return (SByte)ReadByte();
        }

        public Int16 ReadInt16() {
            try {
                Int16 val = BitConverter.ToInt16(buf, offset);
                offset += 2;
                return IPAddress.NetworkToHostOrder(val);
            } catch(Exception e) {
                DLog.Error(e.Message);
                return 0;
            }
        }

        public UInt16 ReadUInt16() {
            return (UInt16)ReadInt16();
        }

        public Int32 ReadInt32() {
            Int32 val = BitConverter.ToInt32(buf, offset);
            offset += 4;
            return IPAddress.NetworkToHostOrder(val);
        }

        public UInt32 ReadUInt32() {
            return (UInt32)ReadInt32();
        }

        public Int64 ReadInt64() {
            Int64 val = BitConverter.ToInt64(buf, offset);
            offset += 8;
            return IPAddress.NetworkToHostOrder(val);
        }

        public UInt64 ReadUInt64() {
            return (UInt64)ReadInt64();
        }

        public Boolean ReadBoolean() {
            Byte val = ReadByte();
            return val == 1;
        }

        public String ReadString() {
            UInt16 size = ReadUInt16();
            byte[] strBuf = new byte[size];
            Array.Copy(buf, offset, strBuf, 0, size);
            offset += size;
            return Encoding.UTF8.GetString(strBuf);
        }

        // write 
        public void Write(byte[] val) {
            if (buf.Length < offset + val.Length) {
                Array.Resize(ref buf, offset * 2 + val.Length);
            }
            Array.Copy(val, 0, buf, offset, val.Length);
            offset += val.Length;
        }

        public void Write(Int16 val) {
            val = IPAddress.HostToNetworkOrder(val);
            byte[] bytes = BitConverter.GetBytes(val);
            Write(bytes);
        }

        public void Write(UInt16 val) {
            Write((Int16)val);
        }

        public void Write(Int32 val) {
            val = IPAddress.HostToNetworkOrder(val);
            byte[] bytes = BitConverter.GetBytes(val);
            Write(bytes);
        }

        public void Write(UInt32 val) {
            Write((Int32)val);
        }

        public void Write(Int64 val) {
            val = IPAddress.HostToNetworkOrder(val);
            byte[] bytes = BitConverter.GetBytes(val);
            Write(bytes);
        }

        public void Write(UInt64 val) {
            Write((Int64)val);
        }

        public void Write(Byte val) {
            byte[] singleByte = new byte[1];
            singleByte[0] = val;
            Write(singleByte);
        }

        public void Write(SByte val) {
            Write((Byte)val);
        }

        public void Write(Boolean val) {
            Write((byte)(val ? 1 : 0));
        }

        public void Write(String val) {
            byte[] str = Encoding.UTF8.GetBytes(val);
            Write((Int16)str.Length);
            Write(str);
        }

        public void WriteBytes2(Byte[] val) {
            Write((Int16)val.Length);
            Write(val);
        }

        public void WriteBytes4(Byte[] val)
        {
            Write((Int32)val.Length);
            Write(val);
        }

        public void WriteSize(int pos, int val) {
    		Int16 tmp_val = IPAddress.HostToNetworkOrder((Int16)val);
    		byte[] bytes = BitConverter.GetBytes(tmp_val);
            Array.Copy(bytes, 0, buf, pos, bytes.Length);
        }


        // Ctrl
        private Ctrl ctrl = new Ctrl();

        public void Skip(int nb) {
            offset += nb;
            return;
        }

        public void SkipRead(int type) {
            int nb = 0;
			if (type<6) { //if ((type&7)<7) {
				nb = TypeBytes[type]; //nb = 1<<(type&7);
            } else {
                nb = ReadUInt16();
            }
            Skip(nb);
        }

        public void SkipWrite(int type) {
            int nb = 0;
			if (type<6) { //if ((type&7)<7) {
				nb = TypeBytes[type]; //nb = 1<<(type&7);
            } else {
                nb = 2;
            }
            Skip(nb);
        }

		public void BindCtrl(Ctrl new_ctrl) {
			ctrl = new_ctrl;
		}

        public void DeEnd() {
            for (int i = ctrl.Num; i < ctrl.Item.Length; i = i + 1) {
                int nb = 0;
                byte type = ctrl.Item[i].Type;
                if (type<6) {
					nb = TypeBytes[type];
                } else {
                    nb = ReadUInt16();
                }
                Skip(nb);
            }
        }

        public void EnEnd() {
            for (int i = ctrl.Num; i < ctrl.Item.Length; i = i + 1) {
				SkipWrite(ctrl.Item [i].Type);
            }
        }

        public bool TryRead(int pos) {
            byte type = ctrl.Item[pos].Type;
			if (ctrl.Item[pos].Ver<1 || ctrl.Item[pos].Ver>Protocol.Ver) {
                SkipRead(type);
                return false;
            } else {
                if (type>=7) {
                    Skip(2);
                }
                return true;
            }
        }

        public bool TryWrite(int pos) {
            byte type = ctrl.Item[pos].Type;
			if (ctrl.Item[pos].Ver<1 || ctrl.Item[pos].Ver>Protocol.Ver) {
                SkipWrite(type);
                return false;
            } else {
                if (type>=7) {
                    //Skip(2);
                }
                return true;
            }
        }
    }
    

    public class Ctrl {
        public class VarItem {
            public byte Type = 0;
            public int Ver = 0;
        }

		public VarItem[] Item = {};
        public int Id = 0;
        public int Num = 0;
    }

	public static class CPool { //var byte
        private static Dictionary<int,Ctrl> New = new Dictionary<int,Ctrl>();
        public static void Reset() {
            New = new Dictionary<int,Ctrl>();
        }
		public static Ctrl Get(int msg_id) {
			if (New.ContainsKey(msg_id)) {
				return New[msg_id];
			} else {
				return new Ctrl();
			}
		}
        
        public static void AddCtrlByStr(short type, string paramStr)
        {
            if (New.ContainsKey(type)) return;
            
            string[] paramsStr = paramStr.TrimEnd(',').Split(',');
            var paramBytes = new byte[paramsStr.Length];
            for (var i = 0; i < paramsStr.Length; i++)
            {
                paramBytes[i] = Convert.ToByte(paramsStr[i]);
            }
        
            var ctrl = new Ctrl
            {
                Id = type,
                Num = paramBytes.Length / 3,
                Item = new Ctrl.VarItem[paramBytes.Length / 3]
            };
        
            var offset = 0;
            var index = 0;
            for (; offset < paramBytes.Length; index++)
            {
                var item = new Ctrl.VarItem();
                item.Type = paramBytes[offset];
                offset++;
                item.Ver = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(paramBytes, offset));
                offset += 2;
                ctrl.Item[index] = item;
            }

            New.Add(ctrl.Id, ctrl);
        }
        
        public static void UpdateCtrlByStr(short type, string paramStr)
        {
            if (!New.ContainsKey(type)) return;
            New.Remove(type);
            AddCtrlByStr(type,paramStr);
        }
        
        public static void Init(byte[] raw) {
            SPack sp = new SPack();
            sp.buf = raw;
            while (!sp.IsTail()) {
                Ctrl c = new Ctrl();
                c.Id = sp.ReadInt16();
                c.Num = sp.ReadInt16();
				c.Item = new Ctrl.VarItem[c.Num];
				//DLog.Error("Ctrl {0} {1}.", c.Id,c.Num);
				for (int i = 0; i < c.Num; i = i + 1) {
					Ctrl.VarItem vi = new Ctrl.VarItem();
					vi.Type = sp.ReadByte();
					vi.Ver = sp.ReadInt16();
					c.Item[i] = vi;
					//DLog.Error("Ctrl item {0} {1}.", i,c.Item[i].Type,c.Item[i].Ver);
                }
                New.Add(c.Id, c);
            }
        }

        public static void Supply(byte[] new_ctrl) {
            SPack sp = new SPack();
            sp.buf = new_ctrl;
			int runver = sp.ReadInt16();
			int dmn = sp.ReadInt16();
			if (Protocol.RunVer < runver) {
				while (!sp.IsTail()) {
					int mid = sp.ReadInt16();
					int dvn = sp.ReadByte(); //diff var num
					bool need_merge = false;
					int var_num = 0;
					if (New.ContainsKey(mid)) {
						need_merge = true;
						var_num = New[mid].Num;
					}

					for (int i = 0; i < dvn; i = i + 1) {
						int vid = sp.ReadByte();
						int vtype = sp.ReadByte();
						int vver = sp.ReadInt16();
						if (need_merge) {
							if (vid >= New[mid].Item.Length) {
								var_num += 1;
								Array.Resize(ref New[mid].Item, var_num);
								New[mid].Item[vid] = new Ctrl.VarItem();
							}
							New[mid].Item[vid].Type = (byte)vtype;
							New[mid].Item[vid].Ver = vver;
						}
					}
				}
				Protocol.RunVer = runver;
			}
        }
    }
}