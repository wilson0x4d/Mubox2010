using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    [DataContract]
    public class CommandInput
        : StationInput
    {
        [DataMember]
        public string Text { get; set; }

        public override string ToString()
        {
            return Text + "/" + base.ToString();
        }
    }
}