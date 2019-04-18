using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RttiPPT;

namespace MinimalDarwinPushPortClientV16
{
    class DarwinMessageHelper
    {
        public static string GetMessageAsString(byte[] bMessage)
        {
            using (MemoryStream oMemoryStream = new MemoryStream(bMessage))
            using (GZipStream oGZipStream = new GZipStream(oMemoryStream, CompressionMode.Decompress))
            using (StreamReader oReader = new StreamReader(oGZipStream))
            {
                return oReader.ReadToEnd();
            }
        }

        public static Pport GetMessageAsObjects(byte[] bMessage)
        {
            using (MemoryStream oMemoryStream = new MemoryStream(bMessage))
            using (GZipStream oGZipStream = new GZipStream(oMemoryStream, CompressionMode.Decompress))
            {
                XmlSerializer oSerializer = new XmlSerializer(typeof(Pport));
                return (Pport)oSerializer.Deserialize(oGZipStream);
            }
        }

        public static string GetMessageDescription(Pport oPPort)
        {
            string sName = oPPort.ItemElementName.ToString();
            PportUR uR = oPPort.Item as PportUR;
            if (uR != null) sName = GetURContentTypes(uR);
            return sName;
        }

        public static string GetURContentTypes(PportUR uR)
        {
            string s = "uR: ";
            if (uR.schedule != null) foreach (var v in uR.schedule) s += "sched " + v.uid + "  ";
            if (uR.association != null) foreach (var v in uR.association) s += "assoc  ";
            if (uR.TS != null) foreach (var v in uR.TS) s += "ts " + v.uid + " ";
            if (uR.alarm != null) foreach (var v in uR.alarm) s += "alarm  ";
            if (uR.deactivated != null) foreach (var v in uR.deactivated) s += "deact  ";
            if (uR.OW != null) foreach (var v in uR.OW) s += "ow  ";
            if (uR.trackingID != null) foreach (var v in uR.trackingID) s += "traId  ";
            if (uR.trainAlert != null) foreach (var v in uR.trainAlert) s += "alert  ";
            if (uR.trainOrder != null) foreach (var v in uR.trainOrder) s += "trOrd  ";
            if (uR.formationLoading != null) foreach (var v in uR.formationLoading) s += "fmnLd  ";
            if (uR.scheduleFormations != null) foreach (var v in uR.scheduleFormations) s += "schFn  ";
            s = s.Trim();
            return s;
        }
    }
}
