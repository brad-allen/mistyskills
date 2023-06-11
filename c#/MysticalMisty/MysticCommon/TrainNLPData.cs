namespace MysticCommon
{
	public class TrainNLPData
	{
		public string Context { get; set; }
		public TrainIntent[] Intents { get; set; } = new TrainIntent[0];
		public bool Save { get; set; }
		public bool Overwrite { get; set; }
	}
}