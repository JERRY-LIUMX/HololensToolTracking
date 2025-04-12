using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEngine.AI;
using UnityEngine.Scripting;
using Unity.XR.CoreUtils;
using System;
public class SampleProcess : MonoBehaviour
{
    //This is the nodes you want to do the data exchange
    public GameObject PubNode, SubNode;
    private DataBuffer PubBuffer, SubBuffer;
    private DebugConsole debugConsole;
    private byte[] message;
    // Start is called before the first frame update
    public GameObject[] tools;
    public Material ellipsoidMaterialTrue,ellipsoidMaterialFalse,ellipsoidMaterialSTTAR;
    public int tool_count
        {
            get { return tools.Length; }
        }
    private float[] tools_coordinates;
    public string[] topics;
    private Vector4 STTARuncertainty = new Vector4(2f,0.01f,0.01f,0.01f);
    void Start()
    {
        //Find the databuffer component for this pub/subnode that you can interact safely from the main thread.
        PubBuffer = PubNode.GetComponent<DataBuffer>();
        SubBuffer = SubNode.GetComponent<DataBuffer>();
        //Find the debugconsole to print the information while in HoloLens.
        debugConsole = GameObject.FindObjectOfType<DebugConsole>();
    }

    private void ScaleEllipsoid(GameObject sphere, Vector4 normalizedVector)
    {
        // Normalize and scale the vector
        // normalizedVector = unnormalizedVector / 0.013f;

        // Apply material if available
        if ((ellipsoidMaterialTrue != null) && (ellipsoidMaterialFalse != null))
        {
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                if((int)normalizedVector[0] == 1)
                {
                    renderer.material = ellipsoidMaterialTrue;
                    sphere.transform.localScale = new Vector3(normalizedVector[1], normalizedVector[2], normalizedVector[3]);
                }
                else if((int)normalizedVector[0] == 0)
                {
                    renderer.material = ellipsoidMaterialFalse;
                    sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
                else if((int)normalizedVector[0] == 2)
                {
                    renderer.material = ellipsoidMaterialSTTAR;
                    sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
            }
        }
    }

    private void VisualizeUncertaintyOf(GameObject parent, Vector4 uncertainty)
    {
            string sphereName = "Uncertainty";
            Transform found = parent.transform.Find(sphereName);

            if (found == null)
            {
                return;
            }
            ScaleEllipsoid(found.gameObject,uncertainty);
    }

    private float[] ObtainToolCoordinates(GameObject tool)
    {
        float[] tool_coordinates = new float[7];
        Matrix4x4 transformMatrix = tool.transform.localToWorldMatrix;
        transformMatrix = FlipTransformRightLeft(transformMatrix);

        // Compute quaternion
        float qw = Mathf.Sqrt(Mathf.Max(0f, 1f + transformMatrix[0, 0] + transformMatrix[1, 1] + transformMatrix[2, 2])) / 2f;
        float qx = Mathf.Sqrt(Mathf.Max(0f, 1f + transformMatrix[0, 0] - transformMatrix[1, 1] - transformMatrix[2, 2])) / 2f;
        float qy = Mathf.Sqrt(Mathf.Max(0f, 1f - transformMatrix[0, 0] + transformMatrix[1, 1] - transformMatrix[2, 2])) / 2f;
        float qz = Mathf.Sqrt(Mathf.Max(0f, 1f - transformMatrix[0, 0] - transformMatrix[1, 1] + transformMatrix[2, 2])) / 2f;

        // Adjust signs
        qx *= (qx * (transformMatrix[2, 1] - transformMatrix[1, 2])) >= 0f ? 1f : -1f;
        qy *= (qy * (transformMatrix[0, 2] - transformMatrix[2, 0])) >= 0f ? 1f : -1f;
        qz *= (qz * (transformMatrix[1, 0] - transformMatrix[0, 1])) >= 0f ? 1f : -1f;

        tool_coordinates[0] = transformMatrix[0, 3];
        tool_coordinates[1] = transformMatrix[1, 3];
        tool_coordinates[2] = transformMatrix[2, 3];
        tool_coordinates[3] = qx;
        tool_coordinates[4] = qy;
        tool_coordinates[5] = qz;
        tool_coordinates[6] = qw;
        return tool_coordinates;
    }

    public Matrix4x4 FlipTransformRightLeft(Matrix4x4 transformRHS)
    {
        // Create a 4x4 "flip" matrix with element-wise flip pattern
        float[,] flipZ = new float[4, 4];

        // Start with ones
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                flipZ[r, c] = 1f;

        // Apply same modifications as in C++
        flipZ[2, 0] = -1f;
        flipZ[0, 2] = -1f;
        flipZ[2, 1] = -1f;
        flipZ[1, 2] = -1f;
        flipZ[2, 3] = -1f;

        // Create a new matrix to store the result
        Matrix4x4 result = new Matrix4x4();

        // Apply element-wise multiplication
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                result[row, col] = transformRHS[row, col] * flipZ[row, col];
            }
        }

        return result;
    }

    private void UpdateToolCoordinates(GameObject tool)
    {
        byte[] poseMessage = SubBuffer.PopMessage(tool.name);
        if(poseMessage == null)
        {
            return;
        }
        string poseString = Encoding.UTF8.GetString(poseMessage);
        // debugConsole.Log(poseString.Split(',').ToString());
        if (tool.tag == "InTrack")
        {   // do not update when the message is not complete
            // debugConsole.Log(tool.name+":"+"Pose message is not complete");
            VisualizeUncertaintyOf(tool,STTARuncertainty);
            return;
        }
        float[] toolCoordinates = poseString.Split(',').Select(float.Parse).ToArray();
        Vector3 position = new Vector3(toolCoordinates[0], toolCoordinates[1], toolCoordinates[2]);
        Quaternion rotation = new Quaternion(toolCoordinates[3], toolCoordinates[4], toolCoordinates[5], toolCoordinates[6]);
        Vector4 uncertainty = new Vector4(toolCoordinates[7],toolCoordinates[8], toolCoordinates[9], toolCoordinates[10]);
        VisualizeUncertaintyOf(tool,uncertainty);
        // tool.transform.localPosition = position;
        // tool.transform.localRotation = rotation;
        // tool.transform.position = position;
        // tool.transform.rotation = rotation;
        Matrix4x4 RightHanded = Matrix4x4.TRS(position, rotation, Vector3.one);
        Matrix4x4 LeftHanded = FlipTransformRightLeft(RightHanded);
        tool.transform.position = LeftHanded.GetColumn(3);
        tool.transform.rotation = Quaternion.LookRotation(
            LeftHanded.GetColumn(2), // Forward
            LeftHanded.GetColumn(1)  // Up
        );
        // Debug.Log(tool.name + " from Server: ");
        // Debug.Log(tool.transform.position);
        // Debug.Log(tool.transform.rotation);
    }

    // public void MatrixToPose(Matirx4x4 transformMatrix, out Vector3 position, out Quaternion rotation)
    // {

    // }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < tool_count; i++){
            tools_coordinates = ObtainToolCoordinates(tools[i]); 
            // Debug.Log(tools[i].name + " from Client: ");
            // Debug.Log(tools[i].transform.position);
            // Debug.Log(tools[i].transform.rotation);
            // Convert the float[] to string.
            string toolPoseString = string.Join(",", tools_coordinates.Select(f => f.ToString("F4")));
            if (tools[i].tag != "InTrack")
            {
                toolPoseString += "," + "0";
            }
            else
            {
                toolPoseString += "," + "1";
            }
            
            //For publishers, you need to give the databuffer the (topic,message) pair. The buffer will enqueue the message and will sent as soon as possible.
            string topic_name = tools[i].name;
            PubBuffer.UpdateOrAddMessage(topic_name, Encoding.UTF8.GetBytes(toolPoseString));

        }
        //For subscribers, you will try to get the message from specific topc using this command. The "try-catch" structure protects your from nullexception
        //since the message from that topic maynot arrived yet..
        // try
        // {
            for (int i = 0; i < tool_count; i++)
            {
                try{
                // string topic = topics[i];
                // string topic = tools[i].name;
                // //Get the message from the topic "topic2" (here is string
                // message = SubBuffer.GetMessage(topic);
                // //Convert the byte[] to your datatype (here is string) and print it in debugConsole.
                // string poseString = Encoding.UTF8.GetString(message);
                // string[] poseValues = poseString.Split(',');
                // if (poseString.Split(',').Length < 7)
                // {   
                //     debugConsole.Log(poseString);
                // }
                // else
                // {
                //     string translationString = $"{poseValues[0]}, {poseValues[1]}, {poseValues[2]}";
                //     string logString = topic + ":" + translationString + ", " + tools[i].tag;
                //     debugConsole.Log(logString);
                // }
                // debugConsole.Log(topic);
                UpdateToolCoordinates(tools[i]);

                // GameObject indicator = tools[i].transform.Find("indicator").gameObject;
                // if(indicator != null)
                // {
                //     Renderer indicator_renderer = indicator.GetComponent<Renderer>();
                //     if (tools[i].tag == "InTrack")
                //     {
                //         indicator_renderer.material.color = Color.green;
                //     }
                //     else
                //     {
                //         indicator_renderer.material.color = Color.red;
                //     }
                // }
            }
            catch{}
            
            
        // }
        // catch
        // {

        // }
    }
}
}
