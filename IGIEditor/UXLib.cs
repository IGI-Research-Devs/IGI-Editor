/* UX Lib by Dark, Contains various elements of U.I Effect,FormMover,Custom Buttons etc.
*/
using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace UXLib
{
    namespace UX
    {
        public class UXWorker
        {
            //<Password Effect - Start>
            Control PasswordEffect_EyeButton;
            TextBox PasswordEffect_PasswordBox;
            public void PasswordEffect(Control EyeButton, TextBox PasswordBox)
            {
                PasswordEffect_EyeButton = EyeButton;
                PasswordEffect_PasswordBox = PasswordBox;

                PasswordBox.UseSystemPasswordChar = true;

                EyeButton.MouseDown += EyeButton_MouseDown;
                EyeButton.MouseUp += EyeButton_MouseUp;
            }

            private void EyeButton_MouseUp(object sender, MouseEventArgs e)
            {
                PasswordEffect_PasswordBox.UseSystemPasswordChar = true;
            }

            private void EyeButton_MouseDown(object sender, MouseEventArgs e)
            {
                PasswordEffect_PasswordBox.UseSystemPasswordChar = false;
            }
            //<Password Effect - End>


            //<Caption Effect - Start>
            TextBox CaptionEffect_TargetBox;
            Color CaptionEffect_CaptionColor;
            Color CaptionEffect_OldColor;
            String CaptionEffect_CaptionText;
            Form CaptionEffect_CurrentForm;
            public void CaptionEffect(TextBox TargetBox, Color CaptionColor, String CaptionText, Form CurrentForm)
            {
                CaptionEffect_TargetBox = TargetBox;
                CaptionEffect_CaptionColor = CaptionColor;
                CaptionEffect_OldColor = TargetBox.ForeColor;
                CaptionEffect_CaptionText = CaptionText;
                CaptionEffect_CurrentForm = CurrentForm;

                TargetBox.ForeColor = CaptionColor;
                TargetBox.Text = CaptionText;
                TargetBox.Click += TargetBox_Click;

                Timer checker = new Timer();
                checker.Enabled = true;
                checker.Interval = 1;
                checker.Tick += Checker_Tick;
                checker.Start();
            }

            private void Checker_Tick(object sender, EventArgs e)
            {
                if (CaptionEffect_CurrentForm.ActiveControl != CaptionEffect_TargetBox && CaptionEffect_TargetBox.Text == "")
                {
                    CaptionEffect_TargetBox.ForeColor = CaptionEffect_CaptionColor;
                    CaptionEffect_TargetBox.Text = CaptionEffect_CaptionText;
                }
            }

            private void TargetBox_Click(object sender, EventArgs e)
            {
                if (CaptionEffect_TargetBox.Text == CaptionEffect_CaptionText && CaptionEffect_TargetBox.ForeColor == CaptionEffect_CaptionColor)
                {
                    CaptionEffect_TargetBox.ForeColor = CaptionEffect_OldColor;
                    CaptionEffect_TargetBox.Text = "";
                }
            }
            //<Caption Effect - End>

            //<Cutom Form Mover - Start>
            bool CustomFormMover_Dragging;
            Point CustomFormMover_Offset;
            Control CustomFormMover_TargetControl;
            Form CustomFormMover_FormToMove;
            public void CustomFormMover(Control TargetControl, Form FormToMove)
            {
                CustomFormMover_TargetControl = TargetControl;
                CustomFormMover_FormToMove = FormToMove;
                TargetControl.MouseDown += TargetControl_MouseDown;
                TargetControl.MouseUp += TargetControl_MouseUp;
                TargetControl.MouseMove += TargetControl_MouseMove;
            }

            private void TargetControl_MouseMove(object sender, MouseEventArgs e)
            {
                if (CustomFormMover_Dragging)
                {
                    Point currentScreenPos = CustomFormMover_FormToMove.PointToScreen(e.Location);
                    CustomFormMover_FormToMove.Location = new
                    Point(currentScreenPos.X - CustomFormMover_Offset.X,
                    currentScreenPos.Y - CustomFormMover_Offset.Y);
                }
            }

            private void TargetControl_MouseUp(object sender, MouseEventArgs e)
            {
                CustomFormMover_Dragging = false;
            }

            private void TargetControl_MouseDown(object sender, MouseEventArgs e)
            {
                CustomFormMover_Dragging = true;
                CustomFormMover_Offset = e.Location;
            }
            //<Cutom Form Mover - End>

            //<Info Viewer - Start>
            Control InfoViewer_TargetControl;
            String InfoViewer_InfoText;
            public void InfoViewer(Control TargetControl, string InfoText)
            {
                InfoViewer_TargetControl = TargetControl;
                InfoViewer_InfoText = InfoText;
                TargetControl.MouseHover += TargetControl_MouseHover;
            }

            private void TargetControl_MouseHover(object sender, EventArgs e)
            {
                ToolTip tt = new ToolTip();
                tt.ToolTipIcon = ToolTipIcon.Info;
                tt.ToolTipTitle = "Info : ";
                tt.SetToolTip(InfoViewer_TargetControl, InfoViewer_InfoText);
            }
            //<Info Viewer - End>

        }
    }

    namespace Text
    {
        class TextWorker
        {
            public string ParseFormat(string TotalString, string FirstString, string LastString)
            {
                int Pos1 = TotalString.IndexOf(FirstString) + FirstString.Length;
                int Pos2 = TotalString.IndexOf(LastString);
                string FinalString = TotalString.Substring(Pos1, Pos2 - Pos1);
                return FinalString;
            }
        }
    }

}
