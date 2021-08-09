using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UDPServerAndWebSocketClient
{
    public class EncryptData
    {

        List<byte> phyPayload = new List<byte>();
        public byte[] encrypt(byte[] payload, string dev_app, byte[] dev_addr, int counter_up)
        {
            byte[] aBlock = { //FRMPayload
                                  0x01,
                                  0x00,
                                  0x00,
                                  0x00,
                                  0x00,
                                  1,// dir - Frame direction ( 0 Uplink or 1 Downlink )
                                  dev_addr[3],
                                  dev_addr[2],
                                  dev_addr[1],
                                  dev_addr[0], //MSB
                                  (byte)(counter_up & 0xff),
                                  (byte)((counter_up >> 8) & 0xff),
                                  (byte)((counter_up >> 16) & 0xff),
                                  (byte)((counter_up >> 24) & 0xff),
                                  0x00,
                                  0x00
            };
            var blocks = Math.Ceiling(payload.Length / 16d);
            List<byte> plain_S = new List<byte>();

            for (var block = 0; block < blocks; block++)
            {
                aBlock[15] = (byte)(block + 1);
                plain_S.AddRange(aBlock);
            }

            var cipherstream = new AES(dev_app).Encrypt(plain_S.ToArray()); // Aes 128

            var encryptData = new List<byte>();

            for (var j = 0; j < payload.Length; j++)
            {
                byte r = (byte)(payload[j] ^ cipherstream[j]);
                encryptData.Add(r);
            }
            return encryptData.ToArray();

        }
       byte [] GetFCnt(int counter)
        {
            byte[] fCounter = new byte[2];
            fCounter[1] = (byte)(counter >> 8);
            fCounter[0] = (byte)counter;
            return fCounter;
        }
        public byte [] CreatMacPayload(byte[] DevAddr, int fCounter, byte[] frmPayload)
        {
            List<byte> macPayload = new List<byte>();
            macPayload.AddRange(DevAddr.Reverse()); //Xem lai ham dao
            macPayload.Add(Convert.ToByte(0x20));//  FCtrl
            macPayload.AddRange(GetFCnt(fCounter));
            macPayload.Add(Convert.ToByte(1));
            macPayload.AddRange(frmPayload);
            return macPayload.ToArray();
        }

        public byte[] CalculateMIC(byte[] DevAddr, int fCnt, byte [] MACPayload, string NwkSKey)
        {
            var msglen = 1 + MACPayload.Length;
            var b0 = new List<byte>();
            b0.AddRange(new byte[] { 0x49, 0x00, 0x00, 0x00, 0x00 });
            b0.Add(1);//Dir=1 downlink
            b0.AddRange(DevAddr.Reverse());
            b0.AddRange(GetFCnt(fCnt));
            b0.AddRange(new byte[] { 0x00, 0x00 });
            b0.Add(0x00);
            b0.Add((byte)msglen);

            var result_B0 = b0.ToArray();

            List<byte> cmac_input = new List<byte>();
            cmac_input.AddRange(result_B0);
            cmac_input.Add(0x60);
            cmac_input.AddRange(MACPayload);

            var fullCAMC = new AESCMAC(NwkSKey).Encrypt(cmac_input.ToArray());

            return  fullCAMC.GetRange(0, 3);
        }



    }
}
