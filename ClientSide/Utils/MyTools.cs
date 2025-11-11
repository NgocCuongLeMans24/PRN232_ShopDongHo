namespace ClientSide.Utils
{
	static public class MyTools
	{
		public static string getUrl()
		{
			return new ConfigurationBuilder().AddJsonFile("appsettings.json")
				.Build()
				.GetValue<string>("GivenAPIBaseUrl");
		}
	}
}
