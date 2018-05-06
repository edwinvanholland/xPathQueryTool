using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace XPathQueryTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string xslStart = @"<?xml version='1.0' encoding='UTF-8'?>
            <xsl:stylesheet version = '1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform' xmlns:s0='{0}'>
            <xsl:template match = '/' > ";

        private string xslEnd = "</xsl:template></xsl:stylesheet>";

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                string defaultInput = File.ReadAllText($"{System.AppDomain.CurrentDomain.BaseDirectory}\\DefaultInput.xml");
                txtXml.Text = defaultInput;

                ReloadTree_Click(this, null);
                GenerateXsl_Click(this, null);
            }
            catch (Exception ex)
            {
                statusStripLabel.Content = ex.Message;
            }
        }

        private void ExecuteXsl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string output = String.Empty;

                // XML result ns0 is now our input, so replace internally to s0
                string input = txtXml.Text;
                input.Replace("<ns0:", "<s0:");

                using (StringReader srt = new StringReader(txtXPathQuery.Text)) // xslInput is a string that contains xsl
                using (StringReader sri = new StringReader(input)) // xmlInput is a string that contains xml
                {
                    using (XmlReader xrt = XmlReader.Create(srt))
                    using (XmlReader xri = XmlReader.Create(sri))
                    {
                        XslCompiledTransform xslt = new XslCompiledTransform();
                        xslt.Load(xrt);
                        using (StringWriter sw = new StringWriter())
                        using (XmlWriter xwo = XmlWriter.Create(sw, xslt.OutputSettings)) // use OutputSettings of xsl, so it can be output as HTML
                        {
                            xslt.Transform(xri, xwo);
                            output = sw.ToString();
                        }
                    }
                }
                txtResult.Text = output;
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(ex.Message);
                if (ex.InnerException != null)
                    sb.AppendLine(ex.InnerException.Message);

                txtResult.Text = sb.ToString();
            }
        }

        private void GenerateXsl_Click(object sender, RoutedEventArgs e)
        {
            Regex rxNs0 = new Regex("xmlns:ns0=\"(.*?)\"");
            if (rxNs0.IsMatch(txtXml.Text))
            {
                string ns0 = rxNs0.Match(txtXml.Text).Groups[1].Value;
                string templateStart = string.Format(xslStart, ns0);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(templateStart);
                sb.AppendLine("");
                sb.AppendLine("<xsl:value-of select='/s0:Message' />");
                sb.AppendLine("");
                sb.Append(xslEnd);

                txtXPathQuery.Text = sb.ToString();
            }
        }

        private void ReloadTree_Click(object sender, RoutedEventArgs e)
        {
            tvXmlTree.Items.Clear();
            try
            {
                using (StringReader sri = new StringReader(txtXml.Text))
                {
                    using (XmlReader xri = XmlReader.Create(sri))
                    {
                        XDocument xDoc = XDocument.Load(xri);

                        TreeViewItem fullTree = AddNodeToTreeView(xDoc.Root, null);
                        tvXmlTree.Items.Add(fullTree);
                        fullTree.IsExpanded = true;
                    }
                }
            }
            catch (Exception ex) {
                tvXmlTree.Items.Add(new TreeViewItem() { Header = ex.Message });
            }
        }

        private TreeViewItem AddNodeToTreeView(XElement element, TreeViewItem parent)
        {
            TreeViewItem leaf = new TreeViewItem() { Header = element.Name.LocalName, IsExpanded=true };
            leaf.ToolTip = element.Value;

            if (parent == null)
            {
                parent = new TreeViewItem() { Header = element.Name.LocalName,};
                parent.Tag = $"/s0:{element.Name.LocalName}";
                leaf.Tag = parent.Tag;
            }
            else
            {
                leaf.Tag = $"{parent.Tag}/s0:{element.Name.LocalName}";
            }


            foreach (XElement subElement in element.Elements())
            {
                leaf.Items.Add(AddNodeToTreeView(subElement, leaf));
            }
            return leaf;
        }

        private void CopyXPath_Click(object sender, EventArgs e)
        {
            if (tvXmlTree.SelectedItem != null)
            {
                System.Windows.Clipboard.SetText((tvXmlTree.SelectedItem as TreeViewItem).Tag.ToString());
                statusStripLabel.Content = $"[{DateTime.Now.ToShortTimeString()}] XPath '{(tvXmlTree.SelectedItem as TreeViewItem).Tag}' copied to clipboard!";
            }
        }

        private void ContextMenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            CopyXPath_Click(sender, e);
        }

        private void ContextMenuReloadTreeView_Click(object sender, RoutedEventArgs e)
        {
            ReloadTree_Click(sender, e);
        }

        private void txtXPathQuery_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ExecuteXsl_Click(sender, e);
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(ex.Message);

                if (ex.InnerException != null)
                    sb.AppendLine(ex.InnerException.Message);

                txtResult.Text = sb.ToString();
            }
        }

        private void txtXml_TextChanged(object sender, TextChangedEventArgs e)
        {
            ReloadTree_Click(this, e);
        }
    }
}
