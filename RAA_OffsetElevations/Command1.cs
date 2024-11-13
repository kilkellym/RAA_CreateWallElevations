using Autodesk.Revit.DB;
using System.Net;
using System.Windows.Controls;

namespace RAA_OffsetElevations
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application and document variables
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            int counter = 0;

            // prompt user to select walls
            List<Reference> curRefs = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element).ToList();

            ViewFamilyType vft = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .Where(x => x.ViewFamily == ViewFamily.Detail)
                        .First();

            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Create section views");

                foreach (Reference curRef in curRefs)
                {
                    Element curElem = doc.GetElement(curRef);

                    if (curElem is not Wall)
                        continue;

                    // get wall and wall curve
                    Wall curWall = curElem as Wall;
                    LocationCurve curLC = curWall.Location as LocationCurve;
                    Curve curCurve = curLC.Curve;

                    // get wall geometry variables
                    XYZ startPt = curCurve.GetEndPoint(0);
                    XYZ endPt = curCurve.GetEndPoint(1);
                    XYZ vector = startPt - endPt;
                    double vLength = vector.GetLength();
                    double offset = 0.08333;
                    double height = curWall.LookupParameter("Unconnected Height").AsDouble();

                    // get bounding box variables
                    XYZ vsBBMin = new XYZ(-vLength / 2, startPt.Z, -offset);
                    XYZ vsBBMax = new XYZ(vLength / 2, height, offset);
                    XYZ midPoint = startPt - (0.5 * vector);
                    XYZ lineDir = vector.Normalize();
                    XYZ up = XYZ.BasisZ;
                    XYZ viewDir = lineDir.CrossProduct(up);

                    // create transform
                    Transform t = Transform.Identity;
                    t.Origin = midPoint;
                    t.BasisX = lineDir;
                    t.BasisY = up;
                    t.BasisZ = viewDir;

                    // create section bounding box and apply transform
                    BoundingBoxXYZ vsBB = new BoundingBoxXYZ();
                    vsBB.Transform = t;
                    vsBB.Min = vsBBMin;
                    vsBB.Max = vsBBMax;

                    // create section 
                    ViewSection newSection = ViewSection.CreateDetail(doc, vft.Id, vsBB);
                    counter++;
                }

                trans.Commit();
            }

            TaskDialog.Show("Complete", $"Created {counter} section views.");

            return Result.Succeeded;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            Common.ButtonDataClass myButtonData = new Common.ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData.Data;
        }

        internal ModelCurve DrawModelLine(Document doc, Curve curve)
        {
            View activeView = doc.ActiveView;
            if (activeView != null)
            {
                SketchPlane sketchPlane = activeView.SketchPlane;
                ModelCurve modelLine = null;
                using (Transaction t = new Transaction(doc, "Create Model Line"))
                {
                    t.Start();
                    modelLine = doc.Create.NewModelCurve(curve, sketchPlane);
                    t.Commit();
                }

                return modelLine;
            }
            return null;
        }
        internal DetailCurve DrawDetailLine(Document doc, Curve curve)
        {
            View activeView = doc.ActiveView;
            if (activeView != null)
            {
                DetailCurve detailLine = null;
                using (Transaction t = new Transaction(doc, "Create Model Line"))
                {
                    t.Start();
                    detailLine = doc.Create.NewDetailCurve(activeView, curve);
                    t.Commit();
                }

                return detailLine;
            }
            return null;
        }
    }

}
