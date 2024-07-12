    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    [Transaction(TransactionMode.Manual)]
    public class SelectObjectsOnLevelCommand : IExternalCommand
    {
        private List<Level> levels;
        private Level selectedLevel;
        private bool selectByLevel;
        private bool selectByReferenceLevel;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Step 1: Get all levels in the document
                Document doc = commandData.Application.ActiveUIDocument.Document;
                levels = GetLevels(doc);

                // Step 2: Show a WPF dialog to select the level or reference level
                if (!ShowSelectionDialog())
                    return Result.Cancelled;

                // Step 3: Filter elements based on the selected criteria
                ICollection<ElementId> selectedElementIds = new List<ElementId>();

                FilteredElementCollector elementCollector = new FilteredElementCollector(doc);

                if (selectByLevel)
                {
                    elementCollector.WherePasses(new ElementLevelFilter(selectedLevel.Id));

                    foreach (var element in elementCollector)
                    {
                        selectedElementIds.Add(element.Id);
                    }
                }

                if (selectByReferenceLevel)
                {
                    foreach (MEPCurve mepCurve in elementCollector.OfClass(typeof(MEPCurve)).Cast<MEPCurve>())
                    {
                        if (mepCurve.ReferenceLevel != null && mepCurve.ReferenceLevel.Id == selectedLevel.Id)
                        {
                            selectedElementIds.Add(mepCurve.Id);
                        }
                    }
                }

                // Step 4: Select the elements
                commandData.Application.ActiveUIDocument.Selection.SetElementIds(selectedElementIds);

                // Step 5: Show a success message
                string messageText = "";

                if (selectByLevel && selectByReferenceLevel)
                {
                    messageText = $"Selected {selectedElementIds.Count} elements on level '{selectedLevel.Name}' and reference level '{selectedLevel.Name}'.";
                }
                else if (selectByLevel)
                {
                    messageText = $"Selected {selectedElementIds.Count} elements on level '{selectedLevel.Name}'.";
                }
                else if (selectByReferenceLevel)
                {
                    messageText = $"Selected {selectedElementIds.Count} elements on reference level '{selectedLevel.Name}'.";
                }

                TaskDialog.Show("Success", messageText);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private List<Level> GetLevels(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Level));

            return collector.Cast<Level>().ToList();
        }

       private bool ShowSelectionDialog()
{
    // Create a WPF window
    Window selectionWindow = new Window
    {
        Title = "Select Level or Reference Level",
        Width = 300,
        Height = 250,
        WindowStartupLocation = WindowStartupLocation.CenterScreen
    };

    // Create a combo box to display the levels
    System.Windows.Controls.ComboBox comboBox = new System.Windows.Controls.ComboBox
    {
        ItemsSource = levels,
        DisplayMemberPath = "Name",
        SelectedIndex = 0,
        Margin = new Thickness(10)
    };

    // Create radio buttons for selecting by level and reference level
    RadioButton levelRadioButton = new RadioButton
    {
        Content = "Select by Level",
        Margin = new Thickness(10),
        IsChecked = true // Default selection
    };

    RadioButton referenceLevelRadioButton = new RadioButton
    {
        Content = "Select by Reference Level",
        Margin = new Thickness(10)
    };

    // Create a button to confirm the selection
    Button confirmButton = new Button
    {
        Content = "Select",
        Width = 70,
        Margin = new Thickness(10)
    };

    // Handle button click event
    confirmButton.Click += (sender, e) =>
    {
        selectedLevel = comboBox.SelectedItem as Level;

        if (selectedLevel == null)
        {
            MessageBox.Show("Please select a level.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        selectByLevel = levelRadioButton.IsChecked == true;
        selectByReferenceLevel = referenceLevelRadioButton.IsChecked == true;

        if (!selectByLevel && !selectByReferenceLevel)
        {
            MessageBox.Show("Please select at least one option (Level or Reference Level).", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        selectionWindow.DialogResult = true;
        selectionWindow.Close();
    };

    // Add the controls to a stack panel
    StackPanel stackPanel = new StackPanel();
    stackPanel.Children.Add(comboBox);
    stackPanel.Children.Add(levelRadioButton);
    stackPanel.Children.Add(referenceLevelRadioButton);
    stackPanel.Children.Add(confirmButton);

    // Set the stack panel as the content of the window
    selectionWindow.Content = stackPanel;

    // Show the window and return the selected option
    return selectionWindow.ShowDialog() == true;
}

    }
