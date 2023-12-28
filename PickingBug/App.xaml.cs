using System.Diagnostics;

namespace PickingBug {

    //SET TEST MODE: can only test gesture recognizer or getVisualTreeElements one at a time (gesture recognizer blocks get VisualTreeElements on Timer otherwise)
    enum TestMode {
        GestureRecognizer,
        GetVisualTreeElements
    }

    //BUG DESCRIPTION:
        //Whether by TapGestureRecognizer or GetVisualTreeElements, Border cannot catch clicks unless it has a background assigned.
        //Set project with three options below (object type, whether background assigned, whether testing by tap gesture or timer running GetVisualTreeElements)
        //Note that you will get 3 outputs for objects found at position in all cases except...
        //If (ObjectToClickType == "Border" && hasBackground == false), the Border is not found

    //BUG PROJECT:
    public partial class App : Application {

        //SELECT OBJECT TYPE TO TRY TO CLICK
        Type ObjectToClickType = typeof(Border); //use AbsoluteLayout, BoxView, or Border (only Border is missed if no background)
        
        //TOGGLE BACKGROUND COLOR
        bool hasBackground = false; // only catches click or getVisualTreeElements for Border if has background color

        //TOGGLE WHICH FUNCTION TO TEST (getVisualTreeElements or GestureRecognizer)
        TestMode testMode = TestMode.GetVisualTreeElements;

        public App() {

            //======================
            //BASIC PAGE CONFIG
            //======================

            //content page
            ContentPage contentPage = new ContentPage();
            contentPage.BackgroundColor = Colors.DarkRed;
            MainPage = contentPage;

            //dummy absolute layout to prevent resizing bug already reported here: https://github.com/dotnet/maui/issues/17883
            AbsoluteLayout abs = new();
            contentPage.Content = abs;

            //build click object
            var clickObject = Activator.CreateInstance(ObjectToClickType);
            abs.Add(clickObject as VisualElement);

            //resize objects to screen
            contentPage.SizeChanged += delegate {
                if (contentPage.Width > 0) {
                    abs.WidthRequest = (clickObject as View).WidthRequest = contentPage.Width;
                    abs.HeightRequest = (clickObject as View).HeightRequest = contentPage.Height;
                }
            };

            //add background coloration
            Color clickObjectColor = Colors.CornflowerBlue;
            if (hasBackground) {
                if (clickObject as BoxView != null) {
                    (clickObject as BoxView).Color = clickObjectColor;
                }
                else if (clickObject as AbsoluteLayout != null) {
                    (clickObject as AbsoluteLayout).BackgroundColor = clickObjectColor;
                }
                else if (clickObject as Border != null) {
                    (clickObject as Border).BackgroundColor = clickObjectColor;
                }
            }

            //========================================
            //TEST GESTURE BASED CLICK CATCHING
            //========================================
            if (testMode == TestMode.GestureRecognizer) {
                TapGestureRecognizer tap = new();
                tap.Tapped += (s, e) => {
                    TappedEventArgs args = e as TappedEventArgs;
                    var position = args.GetPosition(contentPage) ?? new Point(0, 0);
                    Debug.WriteLine("OBJECT TAPPED AT " + position + " AND FOUND:");
                    var picked = contentPage.GetVisualTreeElements(position);
                    foreach (var element in picked) {
                        Debug.WriteLine(element.GetType());
                    }
                };
                (clickObject as View).GestureRecognizers.Add(tap);
            }

            //======================================================================================================
            //TIMER FUNCTION TO TEST "GetVisualTreeElements" ON ITS OWN (ADDING GESTURE BLOCKS THIS OTHERWISE):
            //======================================================================================================
            else {

                var timer = Dispatcher.CreateTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.IsRepeating = true;
                timer.Tick += delegate {
                    var picked = contentPage.GetVisualTreeElements(new Point(100, 100));
                    Debug.WriteLine("TIMER FUNCTION RUN & FOUND: ");
                    foreach (var element in picked) {
                        Debug.WriteLine(element.GetType());
                    }
                };
                timer.Start();
            }


        }
    }
}
