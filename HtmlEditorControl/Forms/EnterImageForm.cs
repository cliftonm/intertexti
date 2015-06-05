#region Using directives

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using ImageAlignOption = MSDN.Html.Editor.ImageAlignOption;

#endregion

namespace MSDN.Html.Editor
{

    /// <summary>
    /// Form used to enter an Html Image attribute
    /// Consists of Href, Text and Image Alignment
    /// </summary>
    internal partial class EnterImageForm : Form
    {

        /// <summary>
        /// Public form constructor
        /// </summary>
        public EnterImageForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            // define the text for the alignment
            this.listAlign.Items.AddRange(Enum.GetNames(typeof(ImageAlignOption)));

            // ensure default value set for target
            this.listAlign.SelectedIndex = 4;

        } //EnterHrefForm


        /// <summary>
        /// Property for the text to display
        /// </summary>
        public string ImageText
        {
            get
            {
                return this.hrefText.Text;
            }
            set
            {
                this.hrefText.Text = value;
            }

        } //ImageText

        /// <summary>
        /// Property for the href for the image
        /// </summary>
        public string ImageLink
        {
            get
            {
                return this.hrefLink.Text.Trim();
            }
            set
            {
                this.hrefLink.Text = value.Trim();
            }

        } //ImageLink

        /// <summary>
        /// Property for the image align
        /// </summary>
        public ImageAlignOption ImageAlign
        {
            get
            {
                return (ImageAlignOption)this.listAlign.SelectedIndex;
            }
            set
            {
                this.listAlign.SelectedIndex = (int)value;
            }
        }

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.RestoreDirectory = true;
			ofd.CheckFileExists = true;
			ofd.Filter = "Image Files (*.bmp, *.jpg, *.gif, *.png)|*.bmp;*.jpg;*.gif;*.png";
			ofd.Title = "Select Image";
			DialogResult res = ofd.ShowDialog();

			if (res == DialogResult.OK)
			{
				string filename = ofd.FileName.Replace(" ", "%20").Replace('\\', '/');
				ImageLink = "file:///" + filename;
			}
		}
    }
}
