namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Models
{
	public static class Const
	{
		public const string RESOURCE_ENTRY = "ResourceEntry";
		public const string UNPARSED_BODY = "UnparsedBody";
		public const string AUTHOR = "Dataphor FHIR 3.0-alpha DSTU-2";

		public const int MAX_HISTORY_RESULT_SIZE = 10000;
		public const int DEFAULT_PAGE_SIZE = 20;

		public static class FhirRestOp
		{
			public const string SNAPSHOT = "_snapshot";
		}

		public static class FhirHeader
		{
			public const string CATEGORY = "Category";
		}

		public static class FhirParameter
		{
			public const string SNAPSHOT_ID = "id";
			public const string SNAPSHOT_INDEX = "start";
			public const string SUMMARY = "_summary";
			public const string COUNT = "_count";
			public const string SINCE = "_since";
			public const string SORT = "_sort";
		}
	}
}