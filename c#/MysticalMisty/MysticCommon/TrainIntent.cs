namespace MysticCommon
{
	public class TrainIntent
	{
		public TrainIntent(string _name, string[] samples)
		{
			Name = _name;
			Samples = samples;
		}

		public string Name { get; set; }
		public string[] Samples { get; set; } = new string[0];
		public object[] Entities { get; set; } = new object[0];
	}
}