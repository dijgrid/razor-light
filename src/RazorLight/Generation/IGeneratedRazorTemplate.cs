using RazorLight.Razor;

namespace RazorLight.Generation
{
	internal interface IGeneratedRazorTemplate
	{
		string TemplateKey { get; }

		string GeneratedCode { get; }

		RazorLightProjectItem ProjectItem { get; set; }
	}
}
