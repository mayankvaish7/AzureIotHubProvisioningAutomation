using System;
using System.Reflection;

namespace Azure.IoTHub.ARMTemplateProvisioning.Areas.HelpPage.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}