using System.Runtime.Serialization;

namespace RemoteEffectCompiler
{
	[DataContract]
	public class ProblemDetails
	{
		[DataMember]
		public string type { get; set; }

		[DataMember]
		public string title { get; set; }

		[DataMember]
		public int? status { get; set; }

		[DataMember]
		public string detail { get; set; }

		[DataMember]
		public string instance { get; set; }
	}
}
