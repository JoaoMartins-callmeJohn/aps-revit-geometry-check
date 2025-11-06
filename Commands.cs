using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using DesignAutomationFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitPlugin
{
	[Transaction(TransactionMode.Manual)]
	[Regeneration(RegenerationOption.Manual)]
	public class Commands : IExternalDBApplication
	{
		//Path of the output file
		string OUTPUT_FILE = Path.Combine("C:\\Users\\martinjoa\\source\\test\\geometries", "geometrycheck.json");

		public ExternalDBApplicationResult OnStartup(ControlledApplication application)
		{
			DesignAutomationBridge.DesignAutomationReadyEvent += HandleDesignAutomationReadyEvent;
			return ExternalDBApplicationResult.Succeeded;
		}

		private void HandleDesignAutomationReadyEvent(object? sender, DesignAutomationReadyEventArgs e)
		{
			LogTrace("Design Automation Ready event triggered...");
			e.Succeeded = true;
			ExportGeometryCheck(e.DesignAutomationData.RevitDoc);
		}

        private void ExportGeometryCheck(Document doc)
        {
            //get families in the project
            List<Element> familySymbolsElements = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).ToElements().ToList();
            JObject jsonObject = new JObject();
            jsonObject.Add("families", new JArray());
            foreach (Element familySymbolElement in familySymbolsElements)
            {
                try{
                    int facesCount = 0;
                    int trianglesCount = 0;
                    FamilySymbol familySymbol = familySymbolElement as FamilySymbol;
                    string name = familySymbol.Family.Name;
                    string category = familySymbol.Family.FamilyCategory.Name;
                    if(familySymbol != null){
                        GeometryElement geometryElement = familySymbol.get_Geometry(new Options(){
                            IncludeNonVisibleObjects = true
                        });
                        if(geometryElement != null){
                            //loop through the geometry elements
                            foreach (GeometryObject geometryObject in geometryElement)
                            {
                                try{
                                    // Check if the current GeometryObject is a Solid
                                    if (geometryObject is Solid solid)
                                    {
                                        // Ensure the solid has faces and volume to filter out empty or invalid solids
                                        if (solid.Faces.Size > 0 && solid.Volume > 0.0)
                                        {
                                            facesCount = solid.Faces.Size;
                                            trianglesCount = 0;
                                            foreach (Face face in solid.Faces)
                                            {
                                                try{
                                                    Mesh mesh = face.Triangulate();
                                                    trianglesCount += mesh.NumTriangles;
                                                }catch(Exception ex){
                                                    LogTrace("Error triangulating face {0}: {1}", face.Id, ex.Message);
                                                }
                                            }
                                        }
                                    }
                                    // Handle GeometryInstances which might contain nested solids (e.g., family instances)
                                    else if (geometryObject is GeometryInstance geomInst)
                                    {
                                        // Recursively get geometry from the instance
                                        GeometryElement instGeomElem = geomInst.GetInstanceGeometry();
                                        foreach (GeometryObject instGeomObj in instGeomElem)
                                        {
                                            if (instGeomObj is Solid nestedSolid)
                                            {
                                                if (nestedSolid.Faces.Size > 0 && nestedSolid.Volume > 0.0)
                                                {
                                                    facesCount = nestedSolid.Faces.Size;
                                                    trianglesCount = 0;
                                                    foreach (Face face in nestedSolid.Faces)
                                                    {
                                                        Mesh mesh = face.Triangulate();
                                                        trianglesCount += mesh.NumTriangles;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }catch(Exception ex){
                                    LogTrace("Error getting geometry for family {0}: {1}", familySymbolElement.Name, ex.Message);
                                }
                            }
                        }
                    }
                    //add the family to the json object
                    JObject familyObject = new JObject{
                        { "familyName", name },
                        { "category", category },
                        { "facesCount", facesCount },
                        { "trianglesCount", trianglesCount }
                    };
                    ((JArray)jsonObject["families"]).Add(familyObject);
                }
                catch(Exception ex){
                    LogTrace("Error getting geometry for family {0}: {1}", familySymbolElement.Name, ex.Message);
                }
            }
            //save jsonObject in the output file
            File.WriteAllText(OUTPUT_FILE, jsonObject.ToString());
        }

		/// <summary>
		/// This will appear on the Design Automation output
		/// </summary>
		private static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }

        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }
    }
}