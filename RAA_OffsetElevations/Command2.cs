namespace RAA_OffsetElevations
{
    [Transaction(TransactionMode.Manual)]
    public class Command2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application and document variables
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Ensure the active view is a detail view
            View originalDetailView = doc.ActiveView;
            //if (originalDetailView == null || originalDetailView.ViewType != ViewType.Detail)
            //{
            //    message = "Please run this command in a detail view.";
            //    return Result.Failed;
            //}

            // Get the ViewFamilyType for a detail view
            ViewFamilyType detailViewFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Detail);

            if (detailViewFamilyType == null)
            {
                message = "No detail view family type found in the document.";
                return Result.Failed;
            }
            
            // Get the view direction of the active view
            //GetViewDirection getOriginalViewDirection = new GetViewDirection();
            //XYZ originalViewDirection = getOriginalViewDirection.GetViewDirectionFromBoundingBox(originalDetailView);
            XYZ originalViewDirection = originalDetailView.ViewDirection;

            // Initialize the XYZ variable to modify the view origin
            XYZ originModifier = XYZ.Zero;

            // Get the bounding box of the original view
            BoundingBoxXYZ originalBB = originalDetailView.get_BoundingBox(originalDetailView);
            double originalBBWidth = originalBB.Max.X;
            XYZ ogOrigin = originalDetailView.Origin;
            XYZ newOrigin = XYZ.Zero;
            double cutPlaneMove = 0;

            // Switch statement to set originModifier based on view direction

            switch (originalViewDirection)
            {
                case XYZ dir when dir.IsAlmostEqualTo(new XYZ(1, 0, 0)):

                    cutPlaneMove = 0.75;

                    XYZ case1 = new XYZ(ogOrigin.X + cutPlaneMove, ogOrigin.Y + originalBBWidth, ogOrigin.Z);

                    newOrigin = case1;

                    break;


                case XYZ dir when dir.IsAlmostEqualTo(new XYZ(-1, 0, 0)):

                    cutPlaneMove = -0.75;

                    XYZ case2 = new XYZ(ogOrigin.X + cutPlaneMove, ogOrigin.Y - originalBBWidth, ogOrigin.Z);

                    newOrigin = case2;

                    break;


                case XYZ dir when dir.IsAlmostEqualTo(new XYZ(0, 1, 0)):

                    cutPlaneMove = 0.75;

                    XYZ case3 = new XYZ(ogOrigin.X - originalBBWidth, ogOrigin.Y + cutPlaneMove, ogOrigin.Z);

                    newOrigin = case3;

                    break;


                case XYZ dir when dir.IsAlmostEqualTo(new XYZ(0, -1, 0)):

                    cutPlaneMove = 2;

                    XYZ case4 = new XYZ(ogOrigin.X + originalBBWidth, ogOrigin.Y + cutPlaneMove, ogOrigin.Z);

                    newOrigin = case4;

                    break;


                default:

                    message = "Unsupported view direction.";

                    return Result.Failed;

            }


                double minZ = originalBB.Min.Z;

                double maxZ = originalBB.Max.Z;

                double w = originalBBWidth;

                double h = maxZ - minZ;


                XYZ newMin = new XYZ(0, 0, minZ);

                XYZ newMax = new XYZ(originalBB.Max.X, originalBB.Max.Y, originalBB.Max.Z);

                XYZ viewHorizontal = originalViewDirection.CrossProduct(XYZ.BasisZ);

                XYZ viewUp = XYZ.BasisZ;

                XYZ viewNormal = viewHorizontal.CrossProduct(viewUp);

                XYZ viewDir = originalViewDirection;


                // Create a transform with the modified origin and desired orientation

                Transform newTransform = Transform.Identity;

                newTransform.Origin = newOrigin;

                newTransform.BasisX = viewHorizontal;

                newTransform.BasisY = viewUp;

                newTransform.BasisZ = viewNormal;


                // Define the new bounding box relative to the new origin

                BoundingBoxXYZ newBB = new BoundingBoxXYZ();

                newBB.Max = originalBB.Max;

                newBB.Min = newMin;

                newBB.Transform = newTransform;


                // Start transaction to create a new detail view

                using (Transaction trans = new Transaction(doc, "Create and Transform Detail View"))

                {

                    trans.Start();


                    // Create the new detail view with the modified bounding box

                    ViewSection newDetailView = ViewSection.CreateDetail(doc, detailViewFamilyType.Id, newBB);

                    if (newDetailView == null)

                    {

                        message = "Failed to create new detail view.";

                        trans.RollBack();

                        return Result.Failed;

                    }


                    newDetailView.Name = originalDetailView.Name + " Test";

                    XYZ newCropMax = new XYZ(originalBB.Max.X, originalBB.Max.Y, 0);

                    XYZ newCropMin = new XYZ(0, 0, minZ);

                    newBB.Max = newCropMax;

                    newBB.Min = newCropMin;

                    newDetailView.CropBox = newBB;


                    newDetailView.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).Set(3);



                    trans.Commit();

                }

                return Result.Succeeded;
        }
        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand2";
            string buttonTitle = "Button 2";

            Common.ButtonDataClass myButtonData = new Common.ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 2");

            return myButtonData.Data;
        }
    }

}
