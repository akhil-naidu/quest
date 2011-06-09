﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace AxeSoftware.Quest.EditorControls
{
    [ControlType("expression")]
    public partial class ExpressionControl : UserControl, IElementEditorControl
    {
        private ControlDataHelper<string> m_helper;
        private Control m_simpleEditor;
        private bool m_simpleMode;
        private bool m_updatingList;
        private bool m_isSimpleModeAvailable = true;
        private bool m_saving = false;
        private IEditorData m_data;
        private bool m_booleanEditor;

        public ExpressionControl()
        {
            InitializeComponent();

            m_helper = new ControlDataHelper<string>(this);
            m_helper.Initialise += Initialise;
            InitialiseInsertMenu();
        }

        void Initialise()
        {
            if (IsSimpleModeAvailable)
            {
                if (m_helper.ControlDefinition.GetString("simple") == null)
                {
                    IsSimpleModeAvailable = false;
                }
            }

            if (IsSimpleModeAvailable)
            {
                string simpleEditor = m_helper.ControlDefinition.GetString("simpleeditor") ?? "textbox";

                m_updatingList = true;

                switch (simpleEditor)
                {
                    case "textbox":
                        TextBox newTextBox = new TextBox();
                        newTextBox.TextChanged += SimpleEditor_TextChanged;
                        newTextBox.LostFocus += SimpleEditor_LostFocus;
                        m_simpleEditor = newTextBox;
                        break;
                    case "file":
                        FileControl newFileControl = new FileControl();
                        newFileControl.Helper.DoInitialise(m_helper.Controller, m_helper.ControlDefinition);
                        newFileControl.RefreshFileList();
                        newFileControl.Helper.Dirty += SimpleEditor_Dirty;
                        newFileControl.lstFiles.SelectionChanged += FileControl_SelectionChanged;
                        m_simpleEditor = newFileControl;
                        break;
                    case "boolean":
                        m_simpleEditor = null;
                        lstType.Items.Add("yes");
                        lstType.Items.Add("no");
                        m_booleanEditor = true;
                        break;
                    case "objects":
                        DropDownObjectsControl newDropDown = new DropDownObjectsControl();
                        newDropDown.Helper.DoInitialise(m_helper.Controller, m_helper.ControlDefinition);
                        newDropDown.Helper.Dirty += SimpleEditor_Dirty;
                        newDropDown.lstDropdown.SelectionChanged += DropDownObjects_SelectionChanged;
                        m_simpleEditor = newDropDown;
                        break;
                    case "number":
                        NumberControl newNumber = new NumberControl();
                        newNumber.Helper.DoInitialise(m_helper.Controller, m_helper.ControlDefinition);
                        newNumber.Helper.Dirty += SimpleEditor_Dirty;
                        newNumber.LostFocus += SimpleEditor_LostFocus;
                        m_simpleEditor = newNumber;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid control type for expression");
                }

                if (m_simpleEditor != null)
                {
                    m_simpleEditor.MinWidth = 40;
                    Grid.SetRow(m_simpleEditor, Grid.GetRow(txtExpression));
                    Grid.SetColumn(m_simpleEditor, Grid.GetColumn(txtExpression));
                    grid.Children.Add(m_simpleEditor);
                }

                if (m_simpleEditor != null)
                {
                    lstType.Items.Add(m_helper.ControlDefinition.GetString("simple"));
                }
                lstType.Items.Add("expression");
                m_updatingList = false;
            }
        }

        void DropDownObjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((DropDownObjectsControl)m_simpleEditor).IsUpdatingList) return;
            SimpleEditor_SelectionChanged();
        }

        void FileControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((FileControl)m_simpleEditor).IsUpdatingList) return;
            SimpleEditor_SelectionChanged();
        }

        private void SimpleEditor_SelectionChanged()
        {
            m_helper.SetDirty(ConvertFromSimpleExpression(SimpleValue));
            Save();
        }

        void SimpleEditor_Dirty(object sender, DataModifiedEventArgs e)
        {
            if (m_simpleEditor is NumberControl)
            {
                m_helper.SetDirty(e.NewValue.ToString());
                Save();
            }
            else
            {
                m_helper.RaiseDirtyEvent(e.NewValue);
            }
        }

        void SimpleEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            Save();
        }

        void SimpleEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_helper.SetDirty(ConvertFromSimpleExpression(SimpleValue));
        }

        public IControlDataHelper Helper
        {
            get { return m_helper; }
        }

        public void Populate(IEditorData data)
        {
            m_data = data;
            if (data == null) return;
            m_helper.StartPopulating();

            string value = m_helper.Populate(data) ?? string.Empty;

            // Don't change the SimpleMode setting if we're just repopulating because this expression has
            // been saved
            if (!m_saving)
            {
                SimpleMode = IsSimpleExpression(value);
            }

            PopulateSimpleControl();

            if (SimpleMode)
            {
                SimpleValue = ConvertToSimpleExpression(value);
            }

            txtExpression.Text = value;

            txtExpression.IsEnabled = m_helper.CanEdit(data);
            txtExpression.IsReadOnly = data.ReadOnly;

            lstType.IsEnabled = !data.ReadOnly;

            // TO DO: Enabled/Readonly state for simple editor

            m_helper.FinishedPopulating();
        }

        private void PopulateSimpleControl()
        {
            if (m_simpleEditor is DropDownObjectsControl)
            {
                ((DropDownObjectsControl)m_simpleEditor).PopulateList();
            }
        }

        public void Save()
        {
            if (!m_helper.IsDirty) return;
            m_saving = true;
            string saveValue = null;
            if (SimpleMode)
            {
                saveValue = ConvertFromSimpleExpression(SimpleValue);
            }
            else
            {
                saveValue = txtExpression.Text;
            }
            m_helper.Save(saveValue);
            m_saving = false;
        }

        private void txtExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_helper.SetDirty(txtExpression.Text);
        }

        private void txtExpression_LostFocus(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private bool SimpleMode
        {
            get { return IsSimpleModeAvailable ? m_simpleMode : false; }
            set
            {
                if (!IsSimpleModeAvailable) return;
                m_simpleMode = value;

                m_updatingList = true;
                if (m_booleanEditor)
                {
                    if (m_simpleMode)
                    {
                        // forces the dropdown to set to the correct value
                        SimpleValue = SimpleValue;
                    }
                    else
                    {
                        lstType.SelectedIndex = 2;
                    }
                }
                else
                {
                    lstType.SelectedIndex = m_simpleMode ? 0 : 1;
                }
                m_updatingList = false;

                Visibility visibility = m_simpleMode ? Visibility.Collapsed : Visibility.Visible;
                txtExpression.Visibility = visibility;
                cmdInsert.Visibility = visibility;

                if (m_simpleEditor != null)
                {
                    ((Control)m_simpleEditor).Visibility = m_simpleMode ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void lstType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_updatingList) return;

            if (m_booleanEditor)
            {
                if (lstType.SelectedIndex <= 1)
                {
                    m_helper.SetDirty(SimpleValue);
                    txtExpression.Text = SimpleValue;
                }
            }

            Save();

            if (m_booleanEditor)
            {
                SimpleMode = (lstType.SelectedIndex <= 1);
            }
            else
            {
                SimpleMode = (lstType.SelectedIndex == 0);
            }

            if (SimpleMode)
            {
                if (IsSimpleExpression(txtExpression.Text))
                {
                    SimpleValue = ConvertToSimpleExpression(txtExpression.Text);
                }
                else
                {
                    SimpleValue = "";
                }
            }
            else
            {
                if (!m_booleanEditor)
                {
                    txtExpression.Text = ConvertFromSimpleExpression(SimpleValue);
                }
            }
        }

        private bool IsSimpleExpression(string expression)
        {
            if (!IsSimpleModeAvailable) return false;

            if (m_booleanEditor)
            {
                return (expression == "true" || expression == "false");
            }

            if (m_simpleEditor is DropDownObjectsControl)
            {
                return expression.Length == 0 || m_helper.Controller.GetObjectNames("object").Contains(expression);
            }

            if (m_simpleEditor is NumberControl)
            {
                int number;
                return int.TryParse(expression, out number);
            }

            // must start and end with quote character
            if (!(expression.StartsWith("\"") && expression.EndsWith("\""))) return false;

            // must not contain a quote character
            return !ConvertToSimpleExpression(expression).Contains("\"");
        }

        private string ConvertToSimpleExpression(string expression)
        {
            if (m_booleanEditor
                || m_simpleEditor is DropDownObjectsControl
                || m_simpleEditor is NumberControl
                )
            {
                return expression;
            }

            return expression.Substring(1, expression.Length - 2);
        }

        private string ConvertFromSimpleExpression(string simpleValue)
        {
            if (m_booleanEditor
                || m_simpleEditor is DropDownObjectsControl
                || m_simpleEditor is NumberControl
                )
            {
                return simpleValue;
            }

            return string.Format("\"{0}\"", simpleValue);
        }

        private string SimpleValue
        {
            get
            {
                if (m_simpleEditor is TextBox)
                {
                    return ((TextBox)m_simpleEditor).Text;
                }
                else if (m_simpleEditor is FileControl)
                {
                    return ((FileControl)m_simpleEditor).Filename;
                }
                else if (m_booleanEditor)
                {
                    return lstType.SelectedIndex == 0 ? "true" : "false";
                }
                else if (m_simpleEditor is DropDownObjectsControl)
                {
                    return ((DropDownObjectsControl)m_simpleEditor).SelectedItem;
                }
                else if (m_simpleEditor is NumberControl)
                {
                    return ((NumberControl)m_simpleEditor).StringValue;
                }
                throw new InvalidOperationException("Unknown control type");
            }
            set
            {
                if (m_simpleEditor is TextBox)
                {
                    ((TextBox)m_simpleEditor).Text = value;
                }
                else if (m_simpleEditor is FileControl)
                {
                    ((FileControl)m_simpleEditor).Filename = value;
                }
                else if (m_booleanEditor)
                {
                    bool oldValue = m_updatingList;
                    m_updatingList = true;
                    lstType.SelectedIndex = (value == "true") ? 0 : 1;
                    m_updatingList = oldValue;
                }
                else if (m_simpleEditor is DropDownObjectsControl)
                {
                    ((DropDownObjectsControl)m_simpleEditor).SelectedItem = value;
                }
                else if (m_simpleEditor is NumberControl)
                {
                    int number;
                    if (!int.TryParse(value, out number))
                    {
                        value = "0";
                    }
                    ((NumberControl)m_simpleEditor).StringValue = value;
                }
                else
                {
                    throw new InvalidOperationException("Unknown control type");
                }
            }
        }

        public bool IsSimpleModeAvailable
        {
            get { return m_isSimpleModeAvailable; }
            set
            {
                m_isSimpleModeAvailable = value;

                lstType.Visibility = m_isSimpleModeAvailable ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void cmdInsert_Click(object sender, RoutedEventArgs e)
        {
            TextOptions.SetTextFormattingMode(mnuInsertMenu, TextFormattingMode.Display);
            mnuInsertMenu.IsOpen = true;
        }

        private void InitialiseInsertMenu()
        {
            AddInsertMenuItem("(Clear)", ClearText);
            mnuInsertMenu.Items.Add(new Separator());
            AddInsertMenuItem("Variable", InsertVariable);
            AddInsertMenuItem("Object", InsertObject);
            AddInsertMenuItem("Property", InsertProperty);
            AddInsertMenuItem("Function", InsertFunction);
            mnuInsertMenu.Items.Add(new Separator());
            AddInsertMenuItem("and", () => InsertString(" and "));
            AddInsertMenuItem("or", () => InsertString(" or "));
            AddInsertMenuItem("+", () => InsertString(" + "));
            AddInsertMenuItem("-", () => InsertString(" - "));
            AddInsertMenuItem("*", () => InsertString(" * "));
            AddInsertMenuItem("/", () => InsertString(" / "));
            AddInsertMenuItem("=", () => InsertString(" = "));
        }

        private void AddInsertMenuItem(string caption, Action insertAction)
        {
            MenuItem newItem = new MenuItem();
            newItem.Header = caption;
            newItem.Click += (object sender, RoutedEventArgs e) => insertAction.Invoke();
            mnuInsertMenu.Items.Add(newItem);
        }

        private void InsertVariable()
        {
            InsertFromList("a variable", m_data.GetVariablesInScope());
        }

        private void InsertObject()
        {
            InsertFromList("an object", m_helper.Controller.GetObjectNames("object", true));
        }

        private void InsertProperty()
        {
            InsertFromList("a property", m_helper.Controller.GetPropertyNames().OrderBy(n => n));
        }

        private void InsertFunction()
        {
            // TO DO: Also need built-in functions defined in WorldModel
            InsertFromList("a function", m_helper.Controller.GetElementNames("function", true).OrderBy(n => n));
        }

        private void InsertFromList(string itemName, IEnumerable<string> items)
        {
            var result = PopupEditors.EditStringWithDropdown(
                string.Format("Please enter {0} name", itemName),
                string.Empty, null, null, "",
                items);

            if (result.Cancelled)
            {
                txtExpression.Focus();
                return;
            }

            InsertString(result.Result);
        }

        private void InsertString(string text)
        {
            int index = txtExpression.SelectionStart;
            txtExpression.Text = txtExpression.Text.Insert(index, text);
            txtExpression.SelectionStart = index + text.Length;
            txtExpression.Focus();
        }

        private void ClearText()
        {
            txtExpression.Text = "";
            txtExpression.Focus();
        }

        public Control FocusableControl
        {
            get
            {
                if (SimpleMode)
                {
                    return m_simpleEditor;
                }
                else
                {
                    return txtExpression;
                }
            }

        }

    }
}
