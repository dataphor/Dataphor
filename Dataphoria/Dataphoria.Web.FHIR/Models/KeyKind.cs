namespace Alphora.Dataphor.Dataphoria.Web.FHIR.Models
{
	public enum KeyKind
	{
		/// <summary>
		/// absolute url, where base is not localhost
		/// </summary>
		Foreign,

		/// <summary>
		/// temporary id, URN, but not a URL. 
		/// </summary>
		Temporary,

		/// <summary>
		/// absolute url, but base is (any of the) localhost(s)
		/// </summary>
		Local,

		/// <summary>
		/// Relative url, for internal references
		/// </summary>
		Internal
	}
}