﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PROProtocol
{
    public static class Encryption
    {
        private static readonly byte[] DEFAULT_SEND_KEY     = { 0xF2, 0x96, 0xA2, 0x3F, 0x90, 0x22, 0x1F, 0xB0, 0x6C, 0x4E, 0x20, 0x65, 0xC3, 0xEF, 0xEC, 0xBF, 0x67, 0x31, 0x31, 0x6B, 0x82, 0x92, 0xAF, 0x57, 0x3C, 0xE9, 0xD6, 0x88, 0x95, 0xA5, 0xC8, 0x7E, 0x78, 0x8F, 0xAC, 0xDF, 0xA2, 0xE0, 0xB5, 0xA4, 0x61, 0x91, 0xD9, 0x7E, 0x52, 0xD2, 0x3B, 0x65, 0x5D, 0xD2, 0x08, 0xCD, 0x3B, 0xC0, 0xCC, 0x13, 0x0A, 0x8F, 0x9B, 0x74, 0x39, 0xBE, 0x0B, 0x8E, 0xCE, 0xE7, 0x74, 0x49, 0xEE, 0x27, 0xCF, 0x8B, 0xD1, 0x73, 0xEC, 0xE2, 0x3E, 0x24, 0x2E, 0x70, 0xC6, 0x41, 0x74, 0x4B, 0x5D, 0x50, 0x56, 0x76, 0x84, 0x5E, 0xF1, 0x71, 0xDC, 0x55, 0xB1, 0x70, 0x62, 0x45, 0x62, 0xA2, 0x45, 0x4C, 0x4D, 0xE8, 0x19, 0x94, 0xB7, 0x0F, 0x54, 0x55, 0xBE, 0x92, 0xEC, 0xEC, 0x15, 0xF8, 0x77, 0x1B, 0x33, 0x45, 0x65, 0x46, 0xCA, 0x4B, 0xA1, 0xA1, 0x45, 0xAC, 0x0D, 0xA2, 0xB1, 0xC6, 0x75, 0x1E, 0x30, 0x87, 0x32, 0xDC, 0x1E, 0x27, 0x06, 0xDF, 0x03, 0x66, 0x8E, 0xC2, 0x6E, 0x06, 0xC4, 0xCD, 0xAA, 0xB1, 0x33, 0x76, 0x65, 0xA9, 0xA8, 0xB9, 0xD1, 0x3D, 0x40, 0xA8, 0xD1, 0x6F, 0xCD, 0x8E, 0x17, 0x07, 0x4F, 0xBB, 0x22, 0x06, 0x6A, 0x4A, 0xA9, 0x82, 0x8A, 0xF0, 0xF9, 0xAC, 0xCB, 0x84, 0xD8, 0x31, 0x36, 0xB5, 0x9A, 0x0F, 0x8D, 0x39, 0xBF, 0x42, 0x1E, 0x6A, 0x7F, 0x63, 0x65, 0xE3, 0x68, 0x4E, 0xE0, 0x76, 0x22, 0x32, 0x84, 0xC2, 0x55, 0xAC, 0xFC, 0xCD, 0x51, 0x78, 0xDE, 0x67, 0x8B, 0x45, 0xBD, 0xF3, 0xA6, 0xFE, 0xE8, 0x27, 0x21, 0x0C, 0xB9, 0x8E, 0x27, 0xB3, 0x77, 0xEA, 0x1A, 0x09, 0xCE, 0x02, 0xAF, 0x66, 0xAA, 0x77, 0x9A, 0x8F, 0xF1, 0x19, 0x9B, 0xE8, 0x5B, 0xF0, 0xA2, 0x54, 0x05, 0xE9, 0xC8, 0x8F, 0x3A, 0x8B, 0x46, 0xBB };

        private static readonly byte[] DEFAULT_RECV_KEY = { 0x29, 0xB4, 0x15, 0xD1, 0x04, 0xA0, 0xF3, 0x0A, 0xF9, 0x99, 0x4B, 0x28, 0xC3, 0x64, 0x7A, 0x01, 0xB7, 0xAE, 0x65, 0x7F, 0xB5, 0x72, 0x87, 0x9B, 0x8C, 0x61, 0xD4, 0x3D, 0xE1, 0x91, 0xF1, 0xC3, 0x48, 0xAF, 0x38, 0x74, 0x36, 0xC2, 0xA9, 0x76, 0xF7, 0xC9, 0x51, 0xDA, 0x5B, 0x0B, 0x14, 0x82, 0x71, 0x5A, 0x7F, 0xC5, 0x7B, 0xDA, 0xD9, 0xF0, 0x17, 0x5E, 0x6F, 0x54, 0xF7, 0x7B, 0x54, 0xB0, 0xC2, 0x8B, 0xCA, 0xB6, 0xB2, 0x05, 0xE9, 0x75, 0x30, 0xB1, 0xE7, 0xCE, 0x59, 0x8F, 0x93, 0x16, 0xA5, 0xC8, 0x87, 0x74, 0x01, 0x22, 0xA5, 0x0B, 0xA5, 0x44, 0x9D, 0x79, 0x8A, 0x39, 0x00, 0x14, 0x29, 0x76, 0xD8, 0x59, 0xDD, 0x5D, 0x59, 0xBF, 0x6C, 0x1A, 0x51, 0xE6, 0xC1, 0x9E, 0xBC, 0x31, 0xB9, 0x8D, 0xBE, 0xC9, 0x53, 0xE5, 0x52, 0x28, 0x39, 0xA6, 0x2F, 0xAF, 0x20, 0x67, 0x9E, 0x4F, 0xD6, 0x6C, 0x70, 0xC5, 0x28, 0x0D, 0xE6, 0x4B, 0x10, 0x02, 0x28, 0xA7, 0xE1, 0x73, 0x2B, 0xA8, 0x8E, 0x0E, 0xC4, 0xE9, 0x0D, 0xB6, 0xA4, 0x30, 0xB1, 0x5A, 0xAC, 0xDB, 0xEC, 0x4A, 0x83, 0x64, 0x92, 0xCD, 0x5D, 0xBE, 0xA3, 0xC2, 0xBD, 0xC2, 0xCC, 0xE6, 0x1D, 0x5F, 0x1F, 0x70, 0x9D, 0xA3, 0x4C, 0x9E, 0x47, 0xE2, 0xAC, 0x19, 0x3B, 0xA3, 0x9D, 0x71, 0x46, 0x43, 0xD4, 0x24, 0x37, 0xFF, 0x1F, 0x70, 0xBF, 0x73, 0x19, 0x3D, 0xB6, 0x06, 0xF1, 0xD3, 0x0B, 0xB9, 0xB5, 0x8F, 0x4E, 0xBD, 0x6E, 0x9B, 0xE8, 0xA5, 0xE4, 0x56, 0xD6, 0xFD, 0xE2, 0x20, 0x51, 0xEF, 0x5A, 0x15, 0xC1, 0xFA, 0xFD, 0x13, 0x93, 0xC3, 0xE3, 0x32, 0xD9, 0x8C, 0xF3, 0x29, 0xC3, 0x9C, 0x9A, 0xAB, 0xD9, 0xDA, 0x61, 0xBB, 0xA6, 0xBD, 0xB2, 0x60, 0x59, 0x28, 0x77, 0x23, 0x5E, 0x73, 0x72, 0xD1, 0x5F, 0x4C };

        private class State
        {
            public State(byte[] box)
            {
                _box = new byte[box.Length];
                Array.Copy(box, _box, box.Length);

                _i = 0;
                _j = 0;
            }

            public byte[] Crypt(IReadOnlyList<byte> bytes, int len)
            {
                byte[] new_bytes = new byte[len];
                var data = bytes.Take(len).ToArray();

                for (int i = 0; i < len; i++)
                {
                    _i = (_i + 1) % 256;
                    _j = (_j + _box[_i]) % 256;

                    byte temp_i = _box[_i];

                    _box[_i] = _box[_j];
                    _box[_j] = temp_i;

                    new_bytes[i] = (byte)(data[i] ^ _box[(_box[_i] + _box[_j]) % 256]);
                }

                return new_bytes;
            }

            private readonly byte[] _box;

            private int _i;
            private int _j;
        }

        private static State recvState;
        private static State sendState;

        public static bool StateReady = false;

        public static byte[] Encrypt(byte[] input, int len = -1)
        {
            return sendState.Crypt(input, len > 0 ? len : input.Length);
        }

        public static byte[] Decrypt(byte[] input, int len = -1)
        {
            return recvState.Crypt(input, len > 0 ? len : input.Length);
        }

        public static void Reset()
        {
            sendState = new State(DEFAULT_SEND_KEY);
            recvState = new State(DEFAULT_RECV_KEY);
            StateReady = false;
        }

        public static string FixDeviceId(Guid deviceId)
        {
            var id = deviceId.ToString();
            // Last byte of the ID is the last byte of the total GUID ASCII Value's Sum
            id = id.Substring(0, id.Length - 2);
            int sum = 0;
            for (int i = 0; i < id.Length; i++)
            {
                sum += id[i];
            }
            id += (sum & 0xff).ToString("X2"); // need the last byte
            return id;
        }
    }
}
