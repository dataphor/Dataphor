namespace Alphora.Dataphor.Dataphoria.Web.Models.DataFhir
{
	public interface IGenerator
	{
		string NextResourceId(string resource);
		string NextVersionId(string resource);
		bool CustomResourceIdAllowed(string value);
	}
}