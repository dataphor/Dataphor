namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Server
{
	public static class DataFhirServerManager
	{
		public static DataFhirServer Instance { get; private set; }
		public static DataFhirCursorManager CursorManager { get; private set; }

		public static void Initialize()
		{
			// TODO: Need to get the instance name configured here...
			Instance = new DataFhirServer();
			CursorManager = new DataFhirCursorManager(Instance.Server);
		}

		public static void Uninitialize()
		{
			if (Instance != null)
			{
				Instance.Dispose();
				Instance = null;
			}
		}
	}
}