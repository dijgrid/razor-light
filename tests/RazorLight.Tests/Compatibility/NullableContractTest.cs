using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Primitives;
using RazorLight.Caching;
using RazorLight.Compilation;
using Xunit;

namespace RazorLight.Tests.Compatibility
{
	public class NullableContractTest
	{
		private static readonly NullabilityInfoContext NullabilityContext = new NullabilityInfoContext();

		[Fact]
		public void Rendering_contract_marks_only_optional_inputs_nullable()
		{
			var method = typeof(IRazorLightEngine).GetMethods()
				.Single(candidate => candidate.Name == nameof(IRazorLightEngine.CompileRenderAsync) && candidate.IsGenericMethod);
			var parameters = method.GetParameters();

			Assert.Equal(NullabilityState.NotNull, NullabilityContext.Create(parameters[0]).WriteState);
			Assert.Equal(NullabilityState.Nullable, NullabilityContext.Create(parameters[2]).WriteState);
			Assert.Equal(typeof(ExpandoObject), parameters[2].ParameterType);
		}

		[Fact]
		public void Lifecycle_and_configuration_contracts_expose_optional_state()
		{
			AssertNullable(typeof(ITemplatePage).GetProperty(nameof(ITemplatePage.PageContext))!);
			AssertNullable(typeof(ITemplatePage).GetProperty(nameof(ITemplatePage.BodyContent))!);
			AssertNullable(typeof(ITemplatePage).GetProperty(nameof(ITemplatePage.Layout))!);
			AssertNullable(typeof(RazorLightOptions).GetProperty(nameof(RazorLightOptions.CachingProvider))!);
			AssertNullable(typeof(RazorLightOptions).GetProperty(nameof(RazorLightOptions.OperatingAssembly))!);
			AssertNullable(typeof(CompiledTemplateDescriptor).GetProperty(nameof(CompiledTemplateDescriptor.Type))!);
		}

		[Fact]
		public void Cache_expiration_token_is_optional()
		{
			var method = typeof(ICachingProvider).GetMethod(nameof(ICachingProvider.CacheTemplate))!;
			var expirationToken = method.GetParameters().Single(parameter => parameter.ParameterType == typeof(IChangeToken));

			Assert.Equal(NullabilityState.Nullable, NullabilityContext.Create(expirationToken).WriteState);
		}

		[Fact]
		public void Caching_enabled_state_guarantees_a_cache()
		{
			var property = typeof(IEngineHandler).GetProperty(nameof(IEngineHandler.IsCachingEnabled))!;
			var attribute = property.GetCustomAttribute<MemberNotNullWhenAttribute>();

			Assert.NotNull(attribute);
			Assert.True(attribute.ReturnValue);
			Assert.Contains(nameof(IEngineHandler.Cache), attribute.Members);
		}

		private static void AssertNullable(PropertyInfo property)
		{
			Assert.Equal(NullabilityState.Nullable, NullabilityContext.Create(property).ReadState);
		}
	}
}
