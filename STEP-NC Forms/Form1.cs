
using STEPNCLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace STEP_NC_Forms
{
    public partial class Form1 : Form
    {
        private double offset;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Finder finder = new Finder();
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "STEP files (*.stp;*.stpnc;*.p21;*.238)|*.stp;*.stpnc;*.p21;*.238|All files (*.*)|*.*";
            if(open.ShowDialog() == DialogResult.OK)
            {
                textBox1.Clear();
                textBox2.Clear();
                string path = open.FileName;
                finder.Open238(path);
                ToolData(finder);
                ToolpathSummary(finder,path);

            }
        }
        private void ToolData(Finder finder)
        {
            long wp_id = finder.GetMainWorkplan();
            long size = finder.GetWorkplanSize(wp_id);
            HashSet<long> used = new HashSet<long>(); //track
            textBox1.AppendText("*TOOL*" + "\r\n");
            for (int i = 0; i < size; i++)
            {
                string WSName = finder.GetWorkingstepName(wp_id, i);
                long ws_id = finder.GetWorkingstep(wp_id, i);
                //long ToolCount = finder.GetWorkplanToolCount(wp_id);
                long ToolId = finder.GetWorkingstepTool(ws_id);
               //long ToolId = finder.GetWorkplanToolNext(wp_id, i);

              if(!used.Contains(ToolId))
                {
                    string ToolType = finder.GetToolType(ws_id);
                    bool value_set;
                    double tool_diameter = finder.GetToolDiameter(ws_id, out value_set);
                    string tool_unit = finder.GetToolDiameterUnit(ws_id);
                    double tool_length = finder.GetToolLength(ws_id, out value_set);
                    double tool_corner_radius = finder.GetToolCornerRadius(ws_id, out value_set);
                    if (tool_corner_radius < 10E-5)
                    {
                        tool_corner_radius = 0;
                    }

                    double tool_tip_angle = finder.GetToolTipAngle(ws_id, out value_set);
                    if (tool_tip_angle < 10E-5)
                    {
                        tool_tip_angle = 0;
                    }
                    textBox1.AppendText(WSName + " | Num.Tool : " + (i + 1) + " | Tool Identifier:" + ToolId + " | Tool Type: " + ToolType + " | Diameter: " + tool_diameter + tool_unit + " | Length: " + tool_length + tool_unit + " | Corner radius: " + tool_corner_radius + tool_unit + " | Tip angle: " + tool_tip_angle + "deg" + "\n");

                }
            }
            textBox1.AppendText("*TOOL*"+"\n"+"\n" + "*PATH*" +"\n");
        }
        private void ToolpathSummary(Finder finder,string path)
        {
            string userName = System.Environment.UserName;
            string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
            string fileName = "ToolpathSummary.txt";
            string fullPath = Path.Combine(downloadFolder, fileName);
            StringBuilder sb = new StringBuilder();
            long wp_id = finder.GetMainWorkplan();
            long size = finder.GetWorkplanSize(wp_id);
           
            for (int i = 0; i < size; i++)
            {
                string WSName = finder.GetWorkingstepName(wp_id, i);
                long ws_id = finder.GetWorkingstep(wp_id, i);
                double Feed = finder.GetProcessFeed(ws_id);
                string Feed_unit = finder.GetProcessFeedUnit(ws_id);
                double Speed = finder.GetProcessSpeed(ws_id);
                string Speed_unit = finder.GetProcessSpeedUnit(ws_id);
                textBox2.AppendText("(" + WSName + " | Tool : " + (i + 1) + " | Feed rate: " + Feed + " " + Feed_unit + " | Spindle: " + Speed + " " + Speed_unit + ")" + "\n" );
                sb.AppendLine("(" + WSName + " | Tool : " + (i + 1) + " | Feed rate: " + Feed + " " + Feed_unit + " | Spindle: " + Speed + " " + Speed_unit + ")" + "\n");
                //File.WriteAllText(fullPath, sb.ToString());
                GetPathID(finder, ws_id,path);             
                File.WriteAllText(fullPath, textBox1.Text + textBox2.Text);               
            }
            MessageBox.Show("ToolpathSummary has been saved at Downloads folder");
            string append = "*PATH*";
            File.AppendAllText(fullPath,append);
        }

        private void GetPathID(Finder finder,long ws_id,string path)
        {
            bool isContact;
            long count = finder.GetWorkingstepPathCount(ws_id);
            string userName = System.Environment.UserName;
            string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
            string fileName = "ToolpathSummary.txt";
            string fullPath = Path.Combine(downloadFolder, fileName);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++ )
            {
                long p_id = finder.GetWorkingstepPathNext(ws_id, i, out isContact);
               
                textBox2.AppendText("(" + "PATH_ID:" + p_id + ")" + "\n");
                sb.AppendLine("(" + "PATH_ID:" + p_id + ")" + "\n");
                //File.AppendAllText(fullPath, sb.ToString());
                DecodePath(finder,(int)p_id,path);
            }
          
        }

        void DecodePath(Finder finder,int path_id,string path)
        {
            long c1 = finder.GetPathCurveCount((long)path_id);
            int count1 = (int)c1;
            long c2 = finder.GetPathAxisCount((long)path_id);
            int count2 = (int)c2;

            if (count2 != 0 && count1 != count2)
            {
                textBox2.AppendText("Error"+ "\n");
            }
            decode_geometry(finder, count1, path_id);

            if (count2 != 0)
            {
                decode_axis(finder, count2, path_id,path);            
            }
            decode_process(finder, path_id);
            decode_axis(finder, count1, path_id,path);  
        }

        void decode_geometry(Finder finder, int count, int path_id)
        {
            string userName = System.Environment.UserName;
            string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
            string fileName = "ToolpathSummary.txt";
            string fullPath = Path.Combine(downloadFolder, fileName);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
               bool isArc;              
               long c_id = finder.GetPathCurveNext(path_id, i, out isArc);
               int cve_id = (int)c_id;
               string curve_type = finder.GetPathCurveType(cve_id);
               double x, y, z;
               finder.GetPathCurveStartPoint(cve_id,out x,out y,out z);                        
               textBox2.AppendText("("+"Curve ID: " + cve_id + " | Curve type is: " + curve_type + ")" +"\n");           
            }
        }

        void decode_axis (Finder finder, int count, int path_id,string path)
        {
            string userName = System.Environment.UserName;
            string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
            string fileName = "ToolpathSummary.txt";
            string fullPath = Path.Combine(downloadFolder, fileName);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                bool isArc;               
                long curve_id = finder.GetPathCurveNext(path_id, i ,out isArc);
                long count_2 = finder.GetPathPolylinePointCount(curve_id);
                textBox2.AppendText("\n"+ "("+ " Number of points: "+ count_2 + " )");

                for (int J = 0; J < count_2; J++)
                {
                    double ai , aj , ak ;
                    finder.GetPathPolylinePointNext(curve_id, J,out ai,out aj,out ak);
                    long ID = finder.GetPathPolylinePointNextID(curve_id,J);
                    string type = GetPointType(ID, path);
                    textBox2.AppendText("\n" + " | x : " + ai + " | y :" + aj + " | z: " + ak + " | Point ID : " + ID + " | Type" + type);
                }             
            }
            textBox2.AppendText("\n" + "\n");
        }

        public string GetPointType(long ID, string path)
        {
            bool foundFirstInstance = false;        
            using (StreamReader reader = new StreamReader(path))
            {               
                string line;
                while ((line = reader.ReadLine()) != null)
                {                 
                    if (line.Contains("#" + ID.ToString()))
                    {
                        if (!foundFirstInstance)
                        {                          
                            foundFirstInstance = true;
                        }
                        else
                        {                           
                            int startIndex = line.IndexOf("#" + ID.ToString()) + ("#" + ID.ToString()).Length;
                            int endIndex = line.IndexOf("(");
                            if (endIndex == -1)
                            {
                                endIndex = line.Length;
                            }
                            string extractedText = "";
                            if (endIndex >= startIndex)
                            {
                                extractedText = line.Substring(startIndex, endIndex - startIndex).Trim();
                            }
                            return extractedText;
                        }
                    }
                }
            }        
            return null;
        }

        void decode_process(Finder finder, int path_id)
        {
            string userName = System.Environment.UserName;
            string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
            string fileName = "ToolpathSummary.txt";
            string fullPath = Path.Combine(downloadFolder, fileName);
            StringBuilder sb = new StringBuilder();
            double feed = 0;
            double speed = 0;
            bool is_rap ;
            bool co_on ;
            finder.GetPathProcess(path_id, out feed ,out speed ,out is_rap,out co_on);
            if(is_rap)
            {
                textBox2.AppendText(" | Rapid path |");
                sb.AppendLine(" | Rapid path |");
            }
            textBox2.AppendText("Feed = " + feed + " |" +" Speed = " + speed + "\n");
        }

        void GetFeature(Finder finder)
        {
            string userName = System.Environment.UserName;
            string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
            string fileName = "ToolpathSummary.txt";
            string fullPath = Path.Combine(downloadFolder, fileName);
            StringBuilder sb = new StringBuilder();
            long count = finder.GetFeatureAllCount();
            textBox2.AppendText(" Number of feature is : " + count +"\n");
            sb.AppendLine(" Number of feature is : " + count + "\n");

            for(int i = 0; i < count; i++)
            {
                long f_id = finder.GetFeatureAllNext(i);
                string type = finder.GetFeatureType(f_id);
                textBox2.AppendText(" Feature type is : " + type + "\n");
            }
        }
       
        private void button2_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
            textBox4.Clear();
            ToolText();

            ToolPath();

            textBox4.AppendText("G49\nM5\nM09\nM30");



        }

        void ToolText()
        {
            try
            {
                string userName = System.Environment.UserName;
                string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
                string fileName = "ToolpathSummary.txt";
                string fullPath = Path.Combine(downloadFolder, fileName);

                if (File.Exists(fullPath))
                {
                    string content = File.ReadAllText(fullPath);
                    string toolContent = GetStringBetween(content, "*TOOL*", "*TOOL*");

                    // parse the toolContent to keep only required information
                    Regex regex = new Regex(@"Num\.Tool\s:\s(\d+).*Tool Type:\s(\w+).*Diameter:\s(\w+mm).*Length:\s(\w+mm).*Tip angle:\s(\w+deg)");
                    StringBuilder sb = new StringBuilder();

                    foreach (Match match in regex.Matches(toolContent))
                    {
                        string toolInfo = $"Num.Tool : {match.Groups[1].Value} | Tool Type: {match.Groups[2].Value} | Diameter: {match.Groups[3].Value} | Length: {match.Groups[4].Value} | Tip angle: {match.Groups[5].Value}";
                        sb.AppendLine(toolInfo);
                    }

                    textBox3.Text = "(" + sb.ToString().Trim() + ")";
                }
                else
                {
                    MessageBox.Show("Summary file not found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occured");
            }
        }

        void ToolPath()
        {
            try
            {
                string userName = System.Environment.UserName;
                string downloadFolder = Path.Combine("C:\\Users", userName, "Downloads");
                string fileName = "ToolpathSummary.txt";
                string fullPath = Path.Combine(downloadFolder, fileName);

                if (File.Exists(fullPath))
                {
                    string content = File.ReadAllText(fullPath);
                    string pathContent = GetStringBetween(content, "*PATH*", "*PATH*");
                    string Gcode = StepToG(pathContent);
                    textBox4.Text = Gcode;
                }
                else
                {
                    MessageBox.Show("Summary file not found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occured");
            }
        }

        private string GetStringBetween(string text, string start, string end)
        {
            int pFrom = text.IndexOf(start) + start.Length;
            int pTo = text.LastIndexOf(end);

            if(pFrom != -1 && pTo != -1 && pTo > pFrom)
            {
                return text.Substring(pFrom, pTo - pFrom);
            }
            return string.Empty;
        }

        private string StepToG(string pathContent)
        {
            var regexTool = new Regex(@"\(point(\d+) WS (\d+) \| Tool : (\d+) \| Feed rate: (\d+) mm\/min \| Spindle: (\d+) rpm\)");
            MatchEvaluator evaluatorTool = match => {
                int toolNumber = int.Parse(match.Groups[3].Value);
                if (toolNumber > 1)
                {
                    return $"M05\nM9\n{match.Value}\nT{match.Groups[3].Value} M6\nM08";
                }
                else
                {
                    return $"G53\nG21\n{match.Value}\nT{match.Groups[3].Value} M6\nM08";
                }
            };
            var regexFeedSpeed = new Regex(@"Feed\s*=\s*(\d+)\s*\|\s*Speed\s*=\s*(\d+)");
            MatchEvaluator evaluator = new MatchEvaluator(replacefeedspeed);
            string newContent = regexFeedSpeed.Replace(pathContent, evaluator);
            newContent = regexFeedSpeed.Replace(pathContent, evaluator);
            newContent = regexTool.Replace(newContent, evaluatorTool);
            //var regexCartesian = new Regex(@"\|\s*x\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*y\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*z\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*Point ID\s*:\s*\d+\s*\|\s*Type=CARTESIAN_POINT");
            var regexCartesian = new Regex(@"\|\s*x\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*y\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*z\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*Point ID\s*:\s*\d+\s*\|\s*Type=CARTESIAN_POINT");
            MatchEvaluator evaluatorCartesian = new MatchEvaluator(replaceCartesian);

            
            newContent = HandlePathContentTrue(newContent);
           
            newContent = regexCartesian.Replace(newContent, evaluatorCartesian);
            newContent = RemoveDuplicate(newContent);

            return newContent;
        }

        private string replacefeedspeed(Match m)
        {
            string feed = m.Groups[1].Value;
            string speed = m.Groups[2].Value;
            string replacement = "M4 S" + speed + "\n" + "F" + feed + "\n" ;
            return replacement;
        }
        private string replaceCartesian(Match m)
        {
            string x = m.Groups[1].Value;
            string y = m.Groups[2].Value;
            string z = (double.Parse(m.Groups[3].Value) + offset).ToString();

            string replacement = "G01 X" + x + " Y" + y + " Z" + z;
            return replacement;
        }
        private static readonly Regex regexCartesian = new Regex(@"\|\s*x\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*y\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*z\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*Point ID\s*:\s*\d+\s*\|\s*Type=CARTESIAN_POINT");
              private static readonly Regex regexArc = new Regex(@"\|\s*x\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*y\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*z\s*:\s*((?:-)?\d+(?:\.\d+)?)\s*\|\s*Point ID\s*:\s*\d+\s*\|\s*Type=VIA_ARC_POINT");

       

        private string HandlePathContentTrue(string pathContent)
        {
            var lines = pathContent.Split('\n');
            StringBuilder sb = new StringBuilder();
            (double x, double y, double z)? arcStart = null;
            List<(double x, double y, double z)> arcPoints = new List<(double x, double y, double z)>();
            foreach (var line in lines)
            {
                var matchCartesian = regexCartesian.Match(line);
                if (matchCartesian.Success)
                {
                    sb.AppendLine(line);  
                    var currentPoint = (double.Parse(matchCartesian.Groups[1].Value), double.Parse(matchCartesian.Groups[2].Value), double.Parse(matchCartesian.Groups[3].Value) + offset);
                    if (arcStart == null)
                    {
                        arcStart = currentPoint;
                    }
                    else
                    {                   
                        if (arcPoints.Count > 0)
                        {
                            var arcEnd = currentPoint;

                            if (arcPoints.Count == 1)
                            {
                                var arcCenter = CalculateArcCenter(arcStart.Value, arcPoints[0], arcEnd);
                                var radius = Math.Sqrt(Math.Pow(arcStart.Value.x - arcCenter.x, 2) + Math.Pow(arcStart.Value.y - arcCenter.y, 2));
                                sb.AppendLine($" G02 X{Round(arcEnd.Item1)} Y{Round(arcEnd.Item2)} Z{Round(arcEnd.Item3)} R{RoundRadius(radius)}");
                            }
                            else if (arcPoints.Count == 2)
                            {
                                var circleCenter = CalculateArcCenter(arcStart.Value, arcPoints[0], arcPoints[1]);
                                var radius = Math.Sqrt(Math.Pow(arcStart.Value.Item1 - circleCenter.Item1, 2) + Math.Pow(arcStart.Value.Item2 - circleCenter.Item2, 2));

                                var oppositePoint = CalculateOppositePoint(circleCenter, arcStart.Value, radius);

                                sb.AppendLine($" G02 X{Round(oppositePoint.Item1)} Y{Round(oppositePoint.Item2)} Z{Round(oppositePoint.Item3)} R{RoundRadius(radius)}");
                                sb.AppendLine($" G02 X{Round(arcEnd.Item1)} Y{Round(arcEnd.Item2)} Z{Round(arcEnd.Item3)} R{RoundRadius(radius)}");
                            }
                            arcStart = null;
                            arcPoints.Clear();
                        }
                    }
                }
                else
                {
                    var matchArc = regexArc.Match(line);
                    if (matchArc.Success)
                    {
                        arcPoints.Add((double.Parse(matchArc.Groups[1].Value), double.Parse(matchArc.Groups[2].Value), double.Parse(matchArc.Groups[3].Value)));
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
            }
            return sb.ToString();
        }

        private string RemoveDuplicate(string pathContent)
        {
            var lines = pathContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            string pattern = @"G0[1-2]\sX([0-9\.-]+)\sY([0-9\.-]+)\sZ([0-9\.-]+)";
            Regex regexCoor = new Regex(pattern);
            (double x, double y, double z)? nextCoordinates = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var match = regexCoor.Match(lines[i]);
                if (match.Success)
                {
                    var currentCoordinates = (double.Parse(match.Groups[1].Value),
                                              double.Parse(match.Groups[2].Value),
                                              double.Parse(match.Groups[3].Value) + offset);

                    if (i < lines.Length - 1)
                    {
                        var nextMatch = regexCoor.Match(lines[i + 1]);
                        if (nextMatch.Success)
                        {
                            nextCoordinates = (double.Parse(nextMatch.Groups[1].Value),
                                               double.Parse(nextMatch.Groups[2].Value),
                                               double.Parse(nextMatch.Groups[3].Value) + offset);
                        }
                        else
                        {
                            nextCoordinates = null;
                        }
                    }

                    if (!currentCoordinates.Equals(nextCoordinates))
                    {
                        sb.AppendLine(lines[i]);
                    }
                }
                else
                {
                    sb.AppendLine(lines[i]);
                }
            }

            return sb.ToString();
        }

        private (double x, double y, double z) CalculateArcCenter((double x, double y, double z) start, (double x, double y, double z) point, (double x, double y, double z) end)
        {
            double ax = start.x;
            double ay = start.y;
            double bx = point.x;
            double by = point.y;
            double cx = end.x;
            double cy = end.y;

            double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));

            double ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
            double uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;

            return (ux, uy, start.z);
        }

        private (double, double, double) CalculateOppositePoint((double x, double y, double z) circleCenter, (double x, double y, double z) startPoint, double radius)
        {
            var dx = startPoint.x - circleCenter.x;
            var dy = startPoint.y - circleCenter.y;
            var dz = startPoint.z - circleCenter.z;
           
            var oppositeX = circleCenter.x - dx;
            var oppositeY = circleCenter.y - dy;
            var oppositeZ = circleCenter.z - dz;

            return (oppositeX, oppositeY, oppositeZ);
        }

        private double Round(double value)
        {
            int decimalPlaces = 3;
            return Math.Round(value,decimalPlaces);
        }

        private double RoundRadius(double value)
        {
            int decimalPlaces = 8;
            return Math.Round(value, decimalPlaces);
        }

        private void ReadSTEPNC()
        {

        }

        private void MakeSTEPNC()
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (double.TryParse(textBox5.Text, out double value))
            {
                offset = value;
            }
            else
            {
                // 
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}

