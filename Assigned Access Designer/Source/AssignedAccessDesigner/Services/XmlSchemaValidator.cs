
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace AssignedAccessDesigner.Services
{
    public static class XmlSchemaValidator
    {
        // Namespace URIs used by the Assigned Access schema family
        private const string NsAssignedAccess2017 = "http://schemas.microsoft.com/AssignedAccess/2017/config";
        private const string NsRs5 = "http://schemas.microsoft.com/AssignedAccess/201810/config";
        private const string NsV3 = "http://schemas.microsoft.com/AssignedAccess/2020/config";
        private const string NsV4 = "http://schemas.microsoft.com/AssignedAccess/2021/config";
        private const string NsV5 = "http://schemas.microsoft.com/AssignedAccess/2022/config";
        private const string NsV3Patch = "http://schemas.microsoft.com/AssignedAccess/202010/config";
        /// <summary>
        /// Validates an Assigned Access XML document against the complete schema set
        /// (2017 + RS5 + v3 + v4 + v5). You can supply either a folder path containing the XSDs
        /// or let the function load embedded resources packaged with the app.
        /// </summary>
        /// <param name="doc">The XDocument to validate.</param>
        /// <param name="localSchemaDirectory">
        /// Optional directory containing the XSD files named:
        ///   AssignedAccess_2017.xsd
        ///   AssignedAccess_201810.xsd
        ///   AssignedAccess_2020.xsd
        ///   AssignedAccess_2021.xsd
        ///   AssignedAccess_2022.xsd
        /// If null or missing, embedded resources will be used.
        /// </param>
        public static void Validate(XDocument doc, string? localSchemaDirectory = null)
        {
            var schemas = BuildCompleteSchemaSet(localSchemaDirectory);

            // Stream validation provides better errors than XDocument.Validate
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemas,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
                                  | XmlSchemaValidationFlags.ProcessSchemaLocation
                                  | XmlSchemaValidationFlags.ProcessInlineSchema
            };

            var errors = new List<string>();
            settings.ValidationEventHandler += (s, e) =>
            {
                var lineInfo = s as IXmlLineInfo;
                var loc = lineInfo?.HasLineInfo() == true
                    ? $" (Line {lineInfo?.LineNumber}, Pos {lineInfo?.LinePosition})"
                    : string.Empty;

                var msg = $"{e.Severity}: {e.Message}{loc}";
                errors.Add(msg);
            };

            using var xmlReader = doc.CreateReader();
            using var validatingReader = XmlReader.Create(xmlReader, settings);

            while (validatingReader.Read()) { /* streaming validate */ }

            if (errors.Count > 0)
            {
                // Aggregate all errors into one exception (keeps call sites simple)
                throw new XmlSchemaValidationException(
                    "Assigned Access XML failed validation:\n" + string.Join("\n", errors));
            }
        }

        /// <summary>
        /// Builds an XmlSchemaSet that contains all imported XSDs and their namespaces.
        /// </summary>
        private static XmlSchemaSet BuildCompleteSchemaSet(string? localSchemaDirectory)
        {
            var schemas = new XmlSchemaSet();

            // Helper to add (namespace, stream) pairs to the set
            void AddSchema(string ns, Stream s)
            {
                var schema = XmlSchema.Read(s, null);
                schema.TargetNamespace = ns; // make sure it's correct
                schemas.Add(schema); // 
            }

            if (!string.IsNullOrEmpty(localSchemaDirectory) && Directory.Exists(localSchemaDirectory))
            {
                // Load from local folder
                AddSchema(NsAssignedAccess2017, File.OpenRead(Path.Combine(localSchemaDirectory, "AssignedAccess_2017.xsd")));
                AddSchema(NsRs5, File.OpenRead(Path.Combine(localSchemaDirectory, "AssignedAccess_201810.xsd")));
                AddSchema(NsV3, File.OpenRead(Path.Combine(localSchemaDirectory, "AssignedAccess_2020.xsd")));
                AddSchema(NsV4, File.OpenRead(Path.Combine(localSchemaDirectory, "AssignedAccess_2021.xsd")));
                AddSchema(NsV5, File.OpenRead(Path.Combine(localSchemaDirectory, "AssignedAccess_2022.xsd")));
                AddSchema(NsV3Patch, File.OpenRead(Path.Combine(localSchemaDirectory, "AssignedAccess_202010.xsd")));
            }
            else
            {
                // Load from embedded resources (add these XSDs to your project as Embedded Resource)
                var asm = Assembly.GetExecutingAssembly();

                Stream GetRes(string name)
                {
                    // Namespace prefix here should match your project’s default namespace
                    var fullName = Array.Find(asm.GetManifestResourceNames(), r => r.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                        ?? throw new FileNotFoundException($"Embedded XSD '{name}' not found in assembly resources.");
                    return asm.GetManifestResourceStream(fullName)
                           ?? throw new FileNotFoundException($"Resource stream for '{name}' not found.");
                }

                AddSchema(NsAssignedAccess2017, GetRes("AssignedAccess_2017.xsd"));
                AddSchema(NsRs5, GetRes("AssignedAccess_201810.xsd"));
                AddSchema(NsV3, GetRes("AssignedAccess_2020.xsd"));
                AddSchema(NsV4, GetRes("AssignedAccess_2021.xsd"));
                AddSchema(NsV5, GetRes("AssignedAccess_2022.xsd"));
                AddSchema(NsV3Patch, GetRes("AssignedAccess_202010.xsd"));
            }

            // Compile once for performance + early detection of schema-level issues
            schemas.Compile();
            return schemas;
        }
    }
}