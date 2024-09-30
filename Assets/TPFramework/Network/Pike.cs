using System;

namespace TPFramework
{
    public class Pike {
        struct Addikey {
            public UInt32 sd;
            public Int32 dis1;
            public Int32 dis2;
            public Int32 index;
            public Int32 carry;
            public UInt32[] buffer;
        }

        UInt32 m_sd;
        Int32 m_index;
        Addikey[] m_addikey = new Addikey[3];
        byte[] m_buffer = new byte[4096];

        const UInt32 GENIUS_NUMBER = 0x05027919;

        public Pike(UInt32 sd)
        {
            m_sd = sd ^ GENIUS_NUMBER;
            m_addikey[0].sd = m_sd;
            m_addikey[0].sd = Linearity(m_addikey[0].sd);
            m_addikey[0].dis1 = 55;
            m_addikey[0].dis2 = 24;
            m_addikey[0].buffer = new UInt32[64];
            m_addikey[1].sd = ((m_sd & 0xAAAAAAAA) >> 1) | ((m_sd & 0x55555555) << 1);
            m_addikey[1].sd = Linearity(m_addikey[1].sd);
            m_addikey[1].dis1 = 57;
            m_addikey[1].dis2 = 7;
            m_addikey[1].buffer = new UInt32[64];
            m_addikey[2].sd = ~(((m_sd & 0xF0F0F0F0) >> 4) | ((m_sd & 0x0F0F0F0F) << 4));
            m_addikey[2].sd = Linearity(m_addikey[2].sd);
            m_addikey[2].dis1 = 58;
            m_addikey[2].dis2 = 19;
            m_addikey[2].buffer = new UInt32[64];

            for (int i = 0; i < 3; ++i) {
                UInt32 tmp = m_addikey[i].sd;

                for (int j = 0; j < 64; j++) {
                    for (int k = 0; k < 32; k++) {
                        tmp = Linearity(tmp);
                    }
                    m_addikey[i].buffer[j] = tmp;
                }
                m_addikey[i].carry = 0;
                m_addikey[i].index = 63;
            }
            m_index = 4096;
        }


        public void Codec(byte[] data, Int32 offset, int length)
        {
            if (length == 0) {
                return;
            }

            while (true) {
                Int32 remnant = 4096 - m_index;
                if (remnant <= 0) {
                    Generate();
                    continue;
                }

                if (remnant > length) {
                    remnant = length;
                }
                length -= remnant;

                for (int i = 0; i < remnant; i++) {
                    data[offset] ^= m_buffer[m_index + i];
                    offset++;
                }
                m_index += remnant;

                if (length <= 0) {
                    break;
                }
            }
        }

        static UInt32 Linearity(UInt32 key)
        {
            return ((((key >> 31) ^ (key >> 6) ^ (key >> 4) ^ (key >> 2) ^ (key >> 1) ^ key) & 0x00000001) << 31) | (key >> 1);
        }

        static void AddikeyNext(ref Addikey addikey)
        {
            var tmp = addikey.index + 1;
            addikey.index = tmp & 0x03F;
            var i1 = ((addikey.index | 0x40) - addikey.dis1) & 0x03F;
            var i2 = ((addikey.index | 0x40) - addikey.dis2) & 0x03F;
            addikey.buffer[addikey.index] = addikey.buffer[i1] + addikey.buffer[i2];
            if ((addikey.buffer[addikey.index] < addikey.buffer[i1]) || (addikey.buffer[addikey.index] < addikey.buffer[i2])) {
                addikey.carry = 1;
            } else {
                addikey.carry = 0;
            }
        }

        private void Generate()
        {
            for (int i = 0; i < 1024; ++i) {
                var carry = m_addikey[0].carry + m_addikey[1].carry + m_addikey[2].carry;
                if (carry == 0 || carry == 3) {
                    AddikeyNext(ref m_addikey[0]);
                    AddikeyNext(ref m_addikey[1]);
                    AddikeyNext(ref m_addikey[2]);
                } else {
                    Int32 flag = 0;
                    if (carry == 2) {
                        flag = 1;
                    }
                    for (int j = 0; j < 3; ++j) {
                        if (m_addikey[j].carry == flag) {
                            AddikeyNext(ref m_addikey[j]);
                        }
                    }
                }
                var tmp = m_addikey[0].buffer[m_addikey[0].index] ^ m_addikey[1].buffer[m_addikey[1].index] ^ m_addikey[2].buffer[m_addikey[2].index];
                var b = i << 2;
                m_buffer[b] = (byte)tmp;
                m_buffer[b + 1] = (byte)(tmp >> 8);
                m_buffer[b + 2] = (byte)(tmp >> 16);
                m_buffer[b + 3] = (byte)(tmp >> 24);
            }

            m_index = 0;
        }
    }
}