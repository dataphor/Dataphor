namespace Alphora.Dataphor.Dataphoria.Web.Core.Models
{
	public interface IGenerator
	{
		string NextResourceId(string resource);
		string NextVersionId(string resource);
		bool CustomResourceIdAllowed(string value);
	}
}