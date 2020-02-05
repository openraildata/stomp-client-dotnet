using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using RttiPPT;

namespace MinimalDarwinPushPortClientV16
{
    internal static class DarwinMessageHelper
    {
        public static Pport GetMessageAsObjects(byte[] bMessage)
        {
            using (var oMemoryStream = new MemoryStream(bMessage))
            using (var oGZipStream = new GZipStream(oMemoryStream, CompressionMode.Decompress))
            {
                var oSerializer = new XmlSerializer(typeof(Pport));
                return (Pport)oSerializer.Deserialize(oGZipStream);
            }
        }

        public static string GetMessageDescription(Pport oPPort)
        {
            var sName = oPPort.ItemElementName.ToString();
            if (oPPort.Item is PportUR uR) sName = GetUrContentTypes(uR);
            return sName;
        }

        private static string GetUrContentTypes(DataResponse uR)
        {
            var s = "uR: ";
            if (uR.schedule != null) s = uR.schedule.Aggregate(s, (current, v) => current + "sched " + v.uid + "  ");
            if (uR.association != null) s = uR.association.Aggregate(s, (current, v) => current + "assoc  ");
            if (uR.TS != null) s = uR.TS.Aggregate(s, (current, v) => current + "ts " + v.uid + " ");
            if (uR.alarm != null) s = uR.alarm.Aggregate(s, (current, v) => current + "alarm  ");
            if (uR.deactivated != null) s = uR.deactivated.Aggregate(s, (current, v) => current + "deact  ");
            if (uR.OW != null) s = uR.OW.Aggregate(s, (current, v) => current + "ow  ");
            if (uR.trackingID != null) s = uR.trackingID.Aggregate(s, (current, v) => current + "traId  ");
            if (uR.trainAlert != null) s = uR.trainAlert.Aggregate(s, (current, v) => current + "alert  ");
            if (uR.trainOrder != null) s = uR.trainOrder.Aggregate(s, (current, v) => current + "trOrd  ");
            if (uR.formationLoading != null) s = uR.formationLoading.Aggregate(s, (current, v) => current + "fmnLd  ");
            if (uR.scheduleFormations != null) s = uR.scheduleFormations.Aggregate(s, (current, v) => current + "schFn  ");
            s = s.Trim();
            return s;
        }
    }
}
