using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IGIEditor
{
    class QGraphs
    {

        public class Graph
        {
            public int x, y, z;

            public Graph() { x = y = z = 0; }
            public Graph(int x)
            {
                this.x = x;
                this.y = this.z = 0;
            }

            public Graph(int x, int y)
            {
                this.x = x;
                this.y = y;
                this.z = 0;
            }

            public Graph(int x, int y, int z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        };

        public class QTaskGraph
        {
            public Int32 id;
            public string name;
            public string note;
            public Real64 position;
            public Graph graph;
            public bool update;
            public bool usePrecise;
            public Int32 graphData;
            public Double midOffsets;
            public Double topOffsets;
            public Double heightDiff;
            public Double nodeWidth;
            public Double groundDist;
            public Double preciseStepVal;
        };

        internal class GraphNode
        {

            int graphId; //Graph Id.
            int nodeMax;//Maximum no. of nodes.
            int nodeTotal;//Total no. of nodes.

            public class VertexNode
            {
                int nodeId; //Current Node id.
                Real64 nodePos; //Node position in Real64.
                float nodeRadius; //Node radius in metre.
                float nodeGamma; //Node gamma angle.
                int nodeMaterial; //Node material like 'Air','Ground','Metal'.
                string nodeCriteria;//Node criteria example Door,Ladder for A.I Graph.

                public VertexNode()
                {
                    this.NodeId = this.nodeMaterial = 0;
                    this.nodeRadius = this.nodeGamma = 0.0f;
                    this.NodeId = 0;
                    this.NodePos = null;
                    this.NodeCriteria = String.Empty;
                }

                public VertexNode(int nodeId, Real64 nodePos, string nodeCriteria)
                {
                    this.NodeId = nodeId;
                    this.NodePos = nodePos;
                    this.nodeCriteria = nodeCriteria;
                }

                public int NodeId { get => nodeId; set => nodeId = value; }
                public string NodeCriteria { get => nodeCriteria; set => nodeCriteria = value; }
                internal Real64 NodePos { get => nodePos; set => nodePos = value; }
            }

            public class EdgeLink
            {
                int nodeLink1;//Node link 1, Connection/Link to 1st node adjacent to it.
                int nodeLink2;//Node link 2, Connection/Link to 2nd node adjacent to it.
                int nodeLinkType;//Link type to other nodes. (Precise) ?

                public EdgeLink()
                {
                    this.nodeLink1 = this.nodeLink2 = this.nodeLinkType = 0;
                }

                public EdgeLink(int nodeLink1, int nodeLink2, int nodeLinkType)
                {
                    this.nodeLink1 = nodeLink1;
                    this.nodeLink2 = nodeLink2;
                    this.nodeLinkType = nodeLinkType;
                }

                public int NodeLink1 { get => nodeLink1; set => nodeLink1 = value; }
                public int NodeLink2 { get => nodeLink2; set => nodeLink2 = value; }
                public int NodeLinkType { get => nodeLinkType; set => nodeLinkType = value; }
            }
            private List<GraphNode.VertexNode> mGraphVertexNodes = new List<VertexNode>();
            private List<GraphNode.EdgeLink> mGraphEdgeLinks = new List<EdgeLink>();

            internal List<EdgeLink> MGraphEdgeLinks { get => mGraphEdgeLinks; set => mGraphEdgeLinks = value; }
            internal List<VertexNode> MGraphVertexNodes { get => mGraphVertexNodes; set => mGraphVertexNodes = value; }
        }


        enum QTASKINFO
        {
            QTASK_ID,
            QTASK_NAME,
            QTASK_NOTE,
            //Object pos in Real64x3.
            QTASK_POSX,
            QTASK_POSY,
            QTASK_POSZ,

            //Graph data.
            QTASK_UPDATE,
            QTASK_GRAPH_DATA_X,
            QTASK_GRAPH_DATA_Y,
            QTASK_GRAPH_DATA_Z,

            //Graph properties in Real64.
            QTASK_MID_OFFSET,
            QTASK_TOP_OFFSET,
            QTASK_HEIGH_DIFF,
            QTASK_NODE_WIDTH,
            QTASK_GROUND_DIST,
            QTASK_USE_PRECISE,
            QTASK_PRECISE_STEP_VAL
        }

        internal static List<int> GetGraphIds(int gameLevel = -1)
        {
            return GetGraphNodeIds(gameLevel);
        }

        internal static Real64 GetGraphPosition(int graphId,int level = -1)
        {
            try
            {
                if (level <= 0)
                {
                    level = QUtils.gGameLevel > 0 ? QUtils.gGameLevel : 1;
                    QLog.AddLog("QGraphs.GetGraphPosition", $"Invalid level received, defaulting to {level}");
                }
                return GetGraphPosition(graphId.ToString(),level);
            }
            catch (Exception ex)
            {
                QLog.LogException("QGraphs.GetGraphPosition", ex);
                return new Real64();
            }
        }

        internal static Real64 GetGraphPosition(string graphId,int level = -1)
        {
            try
            {
                if (level <= 0)
                {
                    level = QUtils.gGameLevel > 0 ? QUtils.gGameLevel : 1;
                    QLog.AddLog("QGraphs.GetGraphPosition", $"Invalid level received, defaulting to {level}");
                }
                var qGraphList = GetQTaskGraphList(true,true,level);
                Real64 qGraphPos = new Real64();

                foreach (var qGraph in qGraphList)
                {
                    if (qGraph.id.ToString() == graphId)
                    {
                        qGraphPos = qGraph.position;
                        break;
                    }
                }
                return qGraphPos;
            }
            catch (Exception ex)
            {
                QLog.LogException("QGraphs.GetGraphPosition", ex);
                return new Real64();
            }
        }


        private static List<int> GetGraphNodeIds(int level = -1)
        {
            List<int> graphNodeIds = new List<int>();
            string graphRegex = "[0-9]{0,4}, \"AIGraph\"";
            string selectedRegex = graphRegex;

            //For current level.
            if (level == -1) level = QMemory.GetRunningLevel();

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with level " + level);

            string inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;
            string qscData = QUtils.LoadFile(inputQscPath);

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "inputQscPath : '" + inputQscPath + "'");

            var matchesResult = Regex.Matches(qscData, selectedRegex).Cast<Match>().Select(m => m.Value).ToList();

            foreach (var matchResult in matchesResult)
            {
                var nodeId = Int32.Parse(Regex.Match(matchResult, @"\d+").Value);
                graphNodeIds.Add(nodeId);
            }

            graphNodeIds.Sort();
            graphNodeIds = graphNodeIds.Distinct().ToList();
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "returned  list count : " + graphNodeIds.Count);
            return graphNodeIds;
        }

        internal static List<KeyValuePair<int, List<int>>> GetNodes4Graph(int level = -1, bool exportData = false, bool exportDetails = false, bool hasAI = true, bool hasGraph = false, bool hasPatrol = false)
        {
            List<KeyValuePair<int, List<int>>> graphNodesList = new List<KeyValuePair<int, List<int>>>();

            //For current level.
            if (level == -1) level = QMemory.GetRunningLevel();
            string graphNodesDetails = "GraphNodesDetails_Level_" + level + ".txt";
            string graphNodesData = "GraphNodesData_Level_" + level + ".txt";

            string inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with level : " + level);
            string data = QUtils.LoadFile(inputQscPath);

            var qscDataLines = data.Split('\n');
            string idData = null;

            foreach (var qscData in qscDataLines)
            {
                if (qscData.Contains("Task_New") && qscData.Contains("HumanAI") && qscData.Contains("AITYPE"))
                {
                    var qscLine = qscData.Split(',');
                    int qscLineLen = qscLine.Length;

                    var aiStr = qscLine[0];
                    var aiId = aiStr.Substring(aiStr.IndexOf("Task_New("));
                    var graphId = qscLine[4];
                    aiId = Regex.Match(aiId, @"\d+").Value;
                    graphId = Regex.Match(graphId, @"\d+").Value;
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Regex aiId : " + aiId + "\tgraphId : " + graphId);


                    string aiFileName = aiId + ".qsc";
                    var inputAiPath = QUtils.cfgQscPath + level + "\\ai\\" + aiFileName;

                    var aiFileData = QUtils.LoadFile(inputAiPath);
                    var aiFileLines = aiFileData.Split('\n');
                    foreach (var aiLine in aiFileLines)
                    {
                        if (aiLine.Contains("AIAction_Patrol"))
                        {
                            var aiPatrolLine = aiLine.Split(',')[0];
                            string patrolId = Regex.Match(aiPatrolLine, @"\d+").Value;
                            if (hasAI) idData += "AI : " + aiId;
                            if (hasGraph) idData += "\tGraphId : " + graphId;
                            if (hasPatrol) idData += "\tPatrolId : " + patrolId;
                            idData += "\n";
                            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Patrol Id : " + patrolId.Trim());
                            var nodeId = GetNodes4mPatrolId(patrolId, level);
                            var iGraphId = Convert.ToInt32(graphId);
                            graphNodesList.Add(new KeyValuePair<int, List<int>>(iGraphId, nodeId));
                        }
                    }
                }
            }
            if (exportDetails)
            {
                QUtils.SaveFile(graphNodesDetails, idData);
            }
            if (exportData)
            {
                idData = null;
                foreach (var graphNode in graphNodesList)
                {
                    idData += "Graph : " + graphNode.Key + "\tNodes : ";
                    foreach (var key in graphNode.Value)
                    {
                        idData += key + ",";
                    }
                    idData += "\n";
                }
                QUtils.SaveFile(graphNodesData, idData);
            }
            return graphNodesList;
        }

        internal static List<int> GetNodes4mPatrolId(string patrolId, int level)
        {
            List<int> nodesId = new List<int>();
            string inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with level : " + level);
            string data = QUtils.LoadFile(inputQscPath);

            var dataLines = data.Split('\n');
            bool patrolLine = false;
            string nodeRegex = "node id [0-9]*";

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with Patrol Id : " + patrolId);


            foreach (var dataLine in dataLines)
            {
                if (dataLine.Contains("PatrolPath") && dataLine.Contains(patrolId))
                {
                    QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Patrol Line : " + patrolLine);
                    patrolLine = true;
                }

                else if (patrolLine && dataLine.Contains("PatrolPathCommand"))
                {
                    if (dataLine.Contains("node id"))
                    {
                        QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Data Line : " + dataLine);
                        var matchesResult = Regex.Matches(dataLine, nodeRegex).Cast<Match>().Select(m => m.Value).ToList();

                        foreach (var matchResult in matchesResult)
                        {
                            var nodeId = Int32.Parse(Regex.Match(matchResult, @"\d+").Value);
                            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Node id : " + nodeId);
                            nodesId.Add(nodeId);
                        }
                    }
                }
                else
                {
                    patrolLine = false;
                }
            }
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Nodes Id count : " + nodesId.Count);
            return nodesId;
        }

		internal static List<int> GetNodesForGraph(int graphId, int level)
		{
			List<int> graphNodeIds = new List<int>();
			string graphFile = Path.Combine(QUtils.cfgGamePath, "missions", "location0", $"level{level}", "graphs", $"graph{graphId}" + QUtils.FileExtensions.Dat);
			
			QUtils.graphNodesList = QGraphs.ReadGraphNodeData(graphFile);
			int totalNodes = QUtils.graphNodesList.Count;

			foreach (var node in QUtils.graphNodesList)
			{
				try
				{
					if (node.MGraphVertexNodes.Count == 0) 
						continue;
					if (node.MGraphVertexNodes.LastOrDefault().NodeId > 0 && node.MGraphVertexNodes.LastOrDefault().NodeId < 5000)
					{
						graphNodeIds.Add(node.MGraphVertexNodes.LastOrDefault().NodeId);
					}
				}
				catch (Exception ex) { }
			}

			QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "GraphFile: '" + graphFile + "'" + " NodeId Count: " + graphNodeIds.Count);
			return graphNodeIds;
		}

        internal static List<int> GetAllNodes4mGraph(int graphId)
        {
            var graphNodesList = GetNodes4Graph();
            List<int> nodeIds = new List<int>();
            foreach (var graphNode in graphNodesList)
            {
                if (graphNode.Key == graphId)
                {
                    foreach (var key in graphNode.Value)
                    {
                        nodeIds.Add(key);
                    }
                }
            }

            if (nodeIds != null)
            {
                if (nodeIds.Count < 1)
                {
                    return null;
                }
                nodeIds.Sort();
                nodeIds = nodeIds.Distinct().ToList();
            }
            return nodeIds;
        }

        internal static List<QTaskGraph> GetQTaskGraphList(bool sorted = false, bool fromBackup = false, int level = -1)
        {
            try
            {
                if (level <= 0)
                {
                    level = QUtils.gGameLevel > 0 ? QUtils.gGameLevel : 1;
                    QLog.AddLog("QGraphs.GetQTaskGraphList", $"Invalid level received, defaulting to {level}");
                }
                //For current level.
                if (level == -1)
                {
                    if (QUtils.gameFound)
                    {
                        level = QMemory.GetRunningLevel();
                    }
                }

                string inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;
                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Called with level : " + level + " backup : " + fromBackup);
                string qscData = fromBackup ? QUtils.LoadFile(inputQscPath) : QUtils.LoadFile();

                var qtaskList = ParseGraphData(qscData);

                if (sorted) qtaskList = qtaskList.OrderBy(q => q.id).ToList();
                return qtaskList;
            }
            catch (Exception ex)
            {
                QLog.LogException("QGraphs.GetQTaskGraphList", ex);
                return new List<QTaskGraph>();
            }
        }

        //Parse the Objects.
        private static List<QTaskGraph> ParseGraphData(string qscData)
        {
            if (String.IsNullOrEmpty(qscData)) return null;
            //Remove all whitespaces.
            qscData = qscData.Replace("\t", String.Empty);
            string[] qscDataSplit = qscData.Split('\n');

            var qtaskList = new List<QTaskGraph>();
            foreach (var data in qscDataSplit)
            {
                if (data.Contains(QUtils.taskNew))
                {
                    var startIndex = data.IndexOf(',') + 1;
                    var endIndex = data.IndexOf(',', startIndex);
                    var taskName = data.Slice(startIndex, endIndex).Trim().Replace("\"", String.Empty);

                    if (taskName.Contains(QUtils.aiGraphTask))
                    {
                        var qtask = new QTaskGraph();
                        Real64 position = new Real64();
                        Graph graph = new Graph();

                        string[] taskNew = data.Split(',');
                        int taskIndex = 0;

                        foreach (var task in taskNew)
                        {
                            if (taskIndex == (int)QTASKINFO.QTASK_ID)
                            {
                                var taskId = task.Substring(task.IndexOf('(') + 1);
                                qtask.id = Convert.ToInt32(taskId);
                            }
                            else if (taskIndex == (int)QTASKINFO.QTASK_NAME)
                                qtask.name = task.Trim();

                            else if (taskIndex == (int)QTASKINFO.QTASK_NOTE)
                                qtask.note = task.Trim();

                            else if (taskIndex == (int)QTASKINFO.QTASK_POSX)
                                position.x = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_POSY)
                                position.y = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_POSZ)
                                position.z = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_UPDATE)
                                qtask.update = Boolean.Parse(task.Trim());

                            else if (taskIndex == (int)QTASKINFO.QTASK_GRAPH_DATA_X)
                                graph.x = Int32.Parse(task.Trim());

                            else if (taskIndex == (int)QTASKINFO.QTASK_GRAPH_DATA_Y)
                                graph.y = Int32.Parse(task.Trim());

                            else if (taskIndex == (int)QTASKINFO.QTASK_GRAPH_DATA_Z)
                                graph.z = Int32.Parse(task.Trim());

                            else if (taskIndex == (int)QTASKINFO.QTASK_MID_OFFSET)
                                qtask.midOffsets = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_TOP_OFFSET)
                                qtask.topOffsets = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_HEIGH_DIFF)
                                qtask.heightDiff = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_NODE_WIDTH)
                                qtask.nodeWidth = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_GROUND_DIST)
                                qtask.groundDist = Double.Parse(task);

                            else if (taskIndex == (int)QTASKINFO.QTASK_USE_PRECISE)
                                qtask.usePrecise = Boolean.Parse(task.Trim());

                            else if (taskIndex == (int)QTASKINFO.QTASK_PRECISE_STEP_VAL)
                                qtask.preciseStepVal = Double.Parse(task.Trim().Replace(")", String.Empty));

                            qtask.position = position;
                            qtask.graph = graph;
                            taskIndex++;
                        }
                        qtaskList.Add(qtask);
                    }
                }
            }
            return qtaskList;
        }

        internal static string ShowGraphVisual(string qscData, int nGraphs = -1)
        {
            int totalGraphs = GetGraphIds().Count, graphCount = 0;
            if (nGraphs == -1) nGraphs = totalGraphs - 1;
            var qtaskList = GetQTaskGraphList(true, true, QUtils.gGameLevel);
            int width = 50000;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "called with nGraphs : " + nGraphs);

            if (nGraphs > totalGraphs)
            {
                QLog.ShowLogError(MethodBase.GetCurrentMethod().Name, "Graph cannot be greater than max graphs");
                return null;
            }

            foreach (var qtask in qtaskList)
            {
                if (graphCount > nGraphs) break;

                qscData += "Task_New(-1, \"Wire\",\"Graph Wire\"," + qtask.position.x + "," + qtask.position.y + "," + qtask.position.z + "," + qtask.position.x + width + "," + qtask.position.y + width + "," + qtask.position.z + width + "," + "\"320_01_1\");" + "\n";
                graphCount++;
            }
            return qscData;
        }

        internal static string ShowGraphNodesVisual(int graphId, QUtils.GRAPH_VISUAL visualType = QUtils.GRAPH_VISUAL.OBJECTS, bool showNodesInfo = false, string nodeObject = "000_00_0", string markerColor = "MARKER_COLOR_YELLOW")
        {
            string qscData = null;
            string graphFile = QUtils.graphsPath + "\\" + "graph" + graphId + QUtils.FileExtensions.Dat;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Graph File: '" + graphFile + "'");
            var nodeData = ReadGraphNodeData(graphFile);
            var graphPos = GetGraphPosition(graphId.ToString());
            QUtils.qtaskId = QTask.GenerateTaskID(true);
            //var graphArea = GetGraphArea(graphId);

            int graphWorkTotal = nodeData.Count, graphWorkCount = 1, graphWorkPercent = 1;

            foreach (var node in nodeData)
            {
                if (node.MGraphVertexNodes.Count == 0) continue;
                if (node.MGraphVertexNodes.LastOrDefault().NodeId < 0 || node.MGraphVertexNodes.LastOrDefault().NodeId > 5000) continue;

                QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Node_" + node.MGraphVertexNodes.LastOrDefault().NodeId + " X: " + node.MGraphVertexNodes.LastOrDefault().NodePos.x + " Y: " + node.MGraphVertexNodes.LastOrDefault().NodePos.y + " Z: " + node.MGraphVertexNodes.LastOrDefault().NodePos.z + " Criteria: " + node.MGraphVertexNodes.LastOrDefault().NodeCriteria);

                var nodeRealPos = new Real64();
                nodeRealPos.x = graphPos.x + node.MGraphVertexNodes.LastOrDefault().NodePos.x;
                nodeRealPos.y = graphPos.y + node.MGraphVertexNodes.LastOrDefault().NodePos.y;
                nodeRealPos.z = graphPos.z + node.MGraphVertexNodes.LastOrDefault().NodePos.z;

                string taskNote = "Graph#" + graphId + "Node#" + node.MGraphVertexNodes.LastOrDefault().NodeId + " - G#" + graphId + "N#1";
                string taskInfo = taskNote.Slice(0, taskNote.IndexOf("-")).Trim();

                //Generate Unique QTaskId's.
                QUtils.qtaskId = QTask.GetUniqueQTaskId(QUtils.qtaskId);

                //Visualisation Object - StatusMsg.
                if (visualType == QUtils.GRAPH_VISUAL.OBJECTS)
                {
                    IGIEditorUI.editorRef.AddRigidObject(nodeObject, true, nodeRealPos, false, -1, taskNote, false);

                    if (showNodesInfo)
                    {
                        var areaDim = new AreaDim(8000);
                        qscData += QObjects.AddAreaActivate(QUtils.qtaskId++, nodeObject, null, "\"" + taskInfo + "\"", ref nodeRealPos, ref areaDim);
                    }
                }

                //Visualisation Hilight - ComputerMap Hilight.
                else if (visualType == QUtils.GRAPH_VISUAL.HILIGHT)
                {
                    IGIEditorUI.editorRef.AddRigidObject(nodeObject, false, nodeRealPos, false, QUtils.qtaskId, taskNote, false);
                    qscData += QObjects.ComputerMapHilight(QUtils.qtaskId++, taskNote, "Graph#" + graphId, taskInfo, "MARKER_BOX", markerColor);
                }
                graphWorkPercent = (int)Math.Round((double)(100 * graphWorkCount) / graphWorkTotal);
                IGIEditorUI.editorRef.SetStatusText("Graph#" + graphId + " Node#" + node.MGraphVertexNodes.LastOrDefault().NodeId + " added, Completed " + graphWorkPercent + "%");
                graphWorkCount++;
            }
            QUtils.SwitchEditorUI();
            return qscData;
        }

        internal static List<string> GraphNodeLinks(int graphId)
        {
            string graphFile = QUtils.graphsPath + "\\" + "graph" + graphId + QUtils.FileExtensions.Dat;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "GraphFile: '" + graphFile + "'");
            var graphData = ReadGraphNodeData(graphFile);
            List<string> nodeLinks = new List<string>();

            foreach (var graph in graphData)
            {
                foreach (var nodeLink in graph.MGraphEdgeLinks)
                {
                    if (nodeLink.NodeLink1 > 0 && nodeLink.NodeLink2 > 0)
                    {
                        string nodeLinkStr = nodeLink.NodeLink1 + "=" + nodeLink.NodeLink2;
                        nodeLinks.Add(nodeLinkStr);
                    }
                }
            }
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "nodeLinks count: '" + nodeLinks.Count + "'");
            return nodeLinks;
        }

        internal static List<GraphNode> ReadGraphNodeData(string graphFile)
        {
            var graphFileData = File.ReadAllBytes(graphFile);
            List<GraphNode> graphs = new List<GraphNode>();
            GraphNode graph = new GraphNode();
            var graphNode = new GraphNode.VertexNode();
            var graphLink = new GraphNode.EdgeLink();
            int nodeLink1, nodeLink2, nodeLinkType;

            for (int index = 0; index < graphFileData.Length; index++)
            {
                //Find Node Vertex - '04CE'
                if (graphFileData[index] == 0x04 && graphFileData[index + 1] == 0xCE)
                {
                    double x, y, z;
                    var nodeIdData = graphFileData.Skip(index + 8).Take(4).ToArray();
                    int nodeId = BitConverter.ToInt32(nodeIdData,0);

                    //Read Node X,Y,Z Position offset.

                    int nodePosXIndex = index + 20;
                    int nodePosYIndex = nodePosXIndex + 8;
                    int nodePosZIndex = nodePosYIndex + 8;

                    var nodePosXData = graphFileData.Skip(nodePosXIndex).Take(8).ToArray();
                    x = BitConverter.ToDouble(nodePosXData,0);

                    var nodePosYData = graphFileData.Skip(nodePosYIndex).Take(8).ToArray();
                    y = BitConverter.ToDouble(nodePosYData,0);

                    var nodePosZData = graphFileData.Skip(nodePosZIndex).Take(8).ToArray();
                    z = BitConverter.ToDouble(nodePosZData,0);

                    //Read node criteria.
                    int nodeCriteriaIndex = index + 88 + 1;
                    var nodeCriteriaData = graphFileData.Skip(nodeCriteriaIndex).Take(18).ToArray();
                    string nodeCriteria = Encoding.UTF8.GetString(nodeCriteriaData);
                    nodeCriteria = (QUtils.IsNonASCII(nodeCriteria) ? String.Empty : nodeCriteria);

                    //Adding Graph Node data.
                    graphNode.NodeId = nodeId;
                    graphNode.NodePos = new Real64(x, y, z);
                    graphNode.NodeCriteria = nodeCriteria;
                    graph.MGraphVertexNodes.Add(graphNode);
                }

                //Find Node Edge(Link1) - '044A'
                else if (graphFileData[index] == 0x04 && graphFileData[index + 1] == 0x4A)
                {
                    nodeLink1 = graphFileData[index + 8];
                    graphLink.NodeLink1 = nodeLink1;
                }

                //Find Node Edge(Link2) - '04F6'

                else if (graphFileData[index] == 0x04 && graphFileData[index + 1] == 0xF6)
                {
                    nodeLink2 = graphFileData[index + 8];
                    graphLink.NodeLink2 = nodeLink2;
                }

                //Find Node Edge(LinkType) - '0423'
                else if (graphFileData[index] == 0x04 && graphFileData[index + 1] == 0x23)
                {
                    nodeLinkType = graphFileData[index + 8];
                    graphLink.NodeLinkType = nodeLinkType;
                    graph.MGraphEdgeLinks.Add(graphLink);
                    graphs.Add(graph);

                    //Reset data for next Graph node.
                    graph = new GraphNode();
                    graphNode = new GraphNode.VertexNode();
                    graphLink = new GraphNode.EdgeLink();
                }
            }
            return graphs;
        }

        internal static string ShowNodeLinksVisual(int graphId, ref Real64 graphPos)
        {
            var dataLines = GraphNodeLinks(graphId); 
            string graphLinkTag;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "dataLines count: '" + dataLines.Count + "'");

            if (dataLines.Count < 4 || dataLines.Count >= 1024) { QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Skipping Graph because of huge size."); return null; }
            int graphWorkTotal = dataLines.Count, graphWorkCount = 1, graphWorkPercent = 1;

            string qscData = null;
            foreach (var data in dataLines)
            {
                try
                {
                    var lineData = data.Split('=');
                    int nodeId1 = Convert.ToInt32(lineData[0].Trim());
                    int nodeId2 = Convert.ToInt32(lineData[1].Trim());
                    var nodeId1Pos = RealGrapNodePos(nodeId1, graphId, ref graphPos);
                    var nodeId2Pos = RealGrapNodePos(nodeId2, graphId, ref graphPos);
                    if (nodeId1Pos.Empty() || nodeId2Pos.Empty()) continue; //Skip On empty positions.

                    graphLinkTag = "Graph#" + graphId + "Link#" + nodeId1 + "-Link#" + nodeId2 + " - G#" + graphId + "L#1";
                    qscData += QObjects.AddWire(nodeId1Pos, nodeId2Pos, graphLinkTag);
                    graphWorkPercent = (int)Math.Round((double)(100 * graphWorkCount) / graphWorkTotal);
                    IGIEditorUI.editorRef.SetStatusText("Graph#" + graphId + " Link#" + graphWorkCount + " added, Completed " + graphWorkPercent + "%");
                    graphWorkCount++;
                }
                catch (Exception ex)
                {
                    QLog.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
                }
            }
            QUtils.SwitchEditorUI();
            return qscData;
        }

        private static Real64 RealGrapNodePos(int nodeId, int graphId, ref Real64 graphPos)
        {
            Real64 nodeRealPos = null;
            try
            {
                var node = QGraphs.GetGraphNodeData(graphId, nodeId);
                //Get Node Real Position.
                //nodeRealPos = new Real64().Real64Operator(graphPos, node.NodePos, "+");
                nodeRealPos = new Real64();
                nodeRealPos.x = graphPos.x + node.MGraphVertexNodes.LastOrDefault().NodePos.x;
                nodeRealPos.y = graphPos.y + node.MGraphVertexNodes.LastOrDefault().NodePos.y;
                nodeRealPos.z = graphPos.z + node.MGraphVertexNodes.LastOrDefault().NodePos.z; //+ 4500.0f;
            }
            catch (Exception ex)
            {
                QLog.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return nodeRealPos;
        }

        internal static bool RemoveNodeLinks(int graphId = 1, int nodeId = 1, string nodeLinkNote = "")
        {
            var qscData = QUtils.LoadFile();
            int linkCount = 0;
            string nodeLinkObj = (nodeLinkNote.Contains("N#")) ? "EditRigidObj" : "Wire";

            string levelFlowTaskId = "Task_New(10,";
            int qtaskLevelFlowIndex = qscData.IndexOf(levelFlowTaskId);

            var qscLevelFlowSub = qscData.Substring(qtaskLevelFlowIndex);
            var qscDataLines = qscLevelFlowSub.Split('\n');

            foreach (var qscDataLine in qscDataLines)
            {
                if (qscDataLine.Contains(nodeLinkObj) && qscDataLine.Contains(nodeLinkNote))
                {
                    int qtaskIndex = qscData.IndexOf(qscDataLine);
                    qscData = qscData.Remove(qtaskIndex, qscDataLine.Length);
                    linkCount++;
                }
            }
            qscData = Regex.Replace(qscData, @"^\s*$", "", RegexOptions.Multiline);
            if (!String.IsNullOrEmpty(qscData)) QCompiler.CompileEx(qscData);
            return (linkCount > 0) ? true : false;
        }


        internal static bool CheckIdExist(string id, string idType, int gameLevel, string errMsg = "")
        {
            bool status = false;
            if (idType == "AI")
                GetNodes4Graph(gameLevel, false, true, true, false, false);

            else if (idType == "Graph")
                GetNodes4Graph(gameLevel, false, true, false, true, false);

            else if (idType == "Patrol")
                GetNodes4Graph(gameLevel, false, true, false, false, true);

            string nodesFile = "GraphNodesDetails_Level_" + gameLevel + ".txt";
            string fileData = QUtils.LoadFile(nodesFile);

            if (Regex.Match(fileData, @"^" + id + "$").Success && idType != "Graph")
            {
                QLog.ShowError(errMsg);
                status = true;
            }
            else if (!Regex.Match(fileData, @"^" + id + "$").Success && idType == "Graph")
            {
                QLog.ShowError(errMsg);
                status = false;
            }
            else if (Regex.Match(fileData, @"^" + id + "$").Success && idType == "Graph")
                status = true;

            QUtils.FileIODelete(nodesFile);
            return status;
        }

        internal static List<KeyValuePair<int, string>> GetGraphAreaList(int level)
        {
            var graphList = GetQTaskGraphList(true, true, level);
            List<KeyValuePair<int, string>> graphNames = new List<KeyValuePair<int, string>>();

            foreach (var graph in graphList)
            {
                if (!String.IsNullOrEmpty(graph.note))
                    graphNames.Add(new KeyValuePair<int, string>(graph.id, graph.note));
            }
            return graphNames;
        }

        internal static string GetGraphArea(int graphId, int level)
        {
            var graphNamesList = GetGraphAreaList(level);
            bool graphIdExist = false;
            string graphName = null;

            foreach (var graph in graphNamesList)
            {
                if (graph.Key == graphId)
                {
                    graphName = graph.Value;
                    graphIdExist = true;
                    break;
                }
            }

            if (!graphIdExist)
            {
                QLog.ShowError("GraphId + " + graphId + " doesn't exist for current level");
                return graphName;
            }
            return graphName;
        }

        internal static string GetGraphArea(int graphId)
        {
            string graphFile = QUtils.qGraphsPath + "\\Areas\\" + "graph_area_level" + QUtils.gGameLevel + QUtils.FileExtensions.Json;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "Level: " + QUtils.gGameLevel + " graphId: " + graphId + " graphFile: " + graphFile);
            if (!System.IO.File.Exists(graphFile)) { return "Area Not Available."; }

            if (QUtils.graphAreas.Count == 0)
            {
                QUtils.graphAreas = GetGraphAreasListJSON(graphFile);
            }

            foreach (var graph in QUtils.graphAreas)
            {
                if (graph.Key == graphId) return graph.Value;
            }
            return null;
        }

        internal static Dictionary<int, string> GetGraphAreasList(string graphFile)
        {
            var fileData = System.IO.File.ReadAllLines(graphFile);
            var graphAreas = new Dictionary<int, string>();
            string areaSub = "Area : ";

            foreach (var data in fileData)
            {
                var graphIdStr = data.Slice(0, data.IndexOf(areaSub)).Trim();
                var graphArea = data.Substring(data.IndexOf(areaSub) + areaSub.Length + 1).Replace("\"", String.Empty).Trim();
                int graphId = Int32.Parse(Regex.Match(graphIdStr, @"\d+").Value);
                graphAreas.Add(graphId, graphArea);
            }
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, " Level: " + QUtils.gGameLevel + " graphFile: " + graphFile + " retured Area count: " + graphAreas.Count);
            return graphAreas;
        }

        static Dictionary<int, string> GetGraphAreasListJSON(string graphFile)
        {
            // Read the data from the JSON file
            string json = File.ReadAllText(graphFile);

            // Deserialize JSON data to list of dictionaries with Graph and Area keys
            List<Dictionary<string, string>> data = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);

            // Convert list of dictionaries to dictionary with Graph ID keys and Area values
            Dictionary<int, string> graphAreas = new Dictionary<int, string>();
            foreach (Dictionary<string, string> dict in data)
            {
                string graph = dict["Graph"];
                int graphId = int.Parse(graph.Substring(graph.IndexOf("#") + 1));
                string area = dict["Area"];
                graphAreas[graphId] = area;
            }

            return graphAreas;
        }


        internal static GraphNode GetGraphNodeData(int graphId, int nodeId)
        {
            string graphFile = QUtils.graphsPath + "\\" + "graph" + graphId + QUtils.FileExtensions.Dat;
            QLog.AddLog(MethodBase.GetCurrentMethod().Name, "GraphFile: '" + graphFile + "'");

            if (QUtils.graphNodesList.Count == 0) QUtils.graphNodesList = QGraphs.ReadGraphNodeData(graphFile);

            foreach (var node in QUtils.graphNodesList)
            {
                if (node.MGraphVertexNodes.Count == 0) continue;
                if (node.MGraphVertexNodes.LastOrDefault().NodeId == nodeId)
                    return node;
            }
            return null;
        }

        internal static void GraphLevelInit(int level,bool showAllGraphsCb,bool initialInit,ref ComboBox aiGraphIdDD)
        {
            QUtils.aiGraphIdStr.Clear();
            QUtils.aiGraphNodeIdStr.Clear();

            var graphIdList = QGraphs.GetGraphIds(level);

            foreach (var graphId in graphIdList)
            {
                //Add all Graphs. (with Cutsceneces).
                if (showAllGraphsCb)
                {
                    QUtils.aiGraphIdStr.Add(graphId);
                    if (initialInit) aiGraphIdDD.Items.Add(graphId);
                }

                //Show Real Graphs only. (without Cutsceneces).
                else if (!showAllGraphsCb)
                {
                    var nodesList = QGraphs.GetNodesForGraph(graphId, level);
                    if (nodesList != null)
                    {
                        if (nodesList.Count > 1)
                        {
                            QUtils.aiGraphIdStr.Add(graphId);
                            if (initialInit) aiGraphIdDD.Items.Add(graphId);
                        }
                    }
                }
            }
        }

        struct AIGraphData
        {
            public int GraphId;
            public int TotalNodes;
            public int MaxNodes;
        }

        private static List<AIGraphData> ExtractAIGraphData(string filePath)
        {
            var aiGraphDataList = new List<AIGraphData>();

            string[] fileContentSplit = File.ReadAllLines(filePath);
            foreach (var data in fileContentSplit)
            {
                if (data.Contains("Task_New") && data.Contains("\"AIGraph\""))
                {
                    var aiGraphData = new AIGraphData();

                    var taskNew = data.Split(',');

                    aiGraphData.GraphId = int.Parse(taskNew[0].Substring(taskNew[0].IndexOf('(') + 1));
                    aiGraphData.TotalNodes = int.Parse(taskNew[7]);
                    aiGraphData.MaxNodes = int.Parse(taskNew[8]);

                    aiGraphDataList.Add(aiGraphData);
                }
            }
            return aiGraphDataList;
        }

        internal static int ExtractNodesData4mQScript(int graphId)
        {
            //For current level.
            int level = QUtils.gGameLevel;

            QLog.AddLog(MethodBase.GetCurrentMethod().Name, $"called with level {level} with graphId {graphId}");

            string inputQscPath = QUtils.cfgQscPath + level + "\\" + QUtils.objectsQsc;

            var aiGraphDataList = ExtractAIGraphData(inputQscPath);
            var aiGraphData = aiGraphDataList.Find(x => x.GraphId == graphId);
            QLog.AddLog(MethodBase.GetCurrentMethod().Name,$"Total Nodes {aiGraphData.TotalNodes} MaxNodes {aiGraphData.MaxNodes}");
            return aiGraphData.TotalNodes;
        }
    }
}
