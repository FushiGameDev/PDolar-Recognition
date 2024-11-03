using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;

using PDollarGestureRecognizer;

public class NeuralNetwork : MonoBehaviour {

	[SerializeField] Transform gestureOnScreenPrefab;

	List<Gesture> trainingSet = new List<Gesture>();
	List<Point> points = new List<Point>();

	Vector3 virtualKeyPosition = Vector2.zero;

	int strokeId = -1;
	int vertexCount = 0;

	LineRenderer currentGestureLineRenderer;
	List<LineRenderer> gestureLinesRenderer = new List<LineRenderer>();
	
	//GUI
	[SerializeField] RectTransform drawArea;
	[SerializeField] TextMeshProUGUI resultText;
	[SerializeField] TMP_InputField inputField;

	bool recognized;
	bool onPanel;
	

	void Start () 
	{
		//Load pre-made gestures
		string[] savedGestures = Directory.GetFiles("Assets/Resources/GestureSet/", "*.xml");
		foreach (string savedXML in savedGestures)
			trainingSet.Add(GestureIO.ReadGestureFromFile(savedXML));
		//Load user custom gestures
		string[] persistentGestures = Directory.GetFiles(Application.persistentDataPath, "*.xml");
		foreach (string persistentXML in persistentGestures)
			trainingSet.Add(GestureIO.ReadGestureFromFile(persistentXML));
	}

	void Update () 
	{
		if (Input.touchCount > 0)		
			virtualKeyPosition = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
		if (Input.GetMouseButton(0))	
			virtualKeyPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y); // debug
	}

	public void Recognize()
    {
		recognized = true;

		Gesture candidate = new Gesture(points.ToArray());
		Result gestureResult = PointCloudRecognizer.Classify(candidate, trainingSet.ToArray());

        if (gestureResult.Score > 0.8f)
			resultText.color = Color.green;
		else
			resultText.color = Color.red;
		resultText.text = gestureResult.GestureClass + " " + gestureResult.Score;

	}

	public void Add()
	{
		var newGestureName = inputField.text;
		if (points.Count > 0 && newGestureName != "")
        {
			string fileName = String.Format("{0}/{1}-{2}.xml", Application.persistentDataPath, newGestureName, DateTime.Now.ToFileTime());
			#if !UNITY_WEBPLAYER
						GestureIO.WriteGesture(points.ToArray(), newGestureName, fileName);
			#endif
			trainingSet.Add(new Gesture(points.ToArray(), newGestureName));
			inputField.text = "";
		}
	}

	public void Dragging()
    {
		if (Input.GetMouseButton(0) && onPanel)
		{
			points.Add(new Point(virtualKeyPosition.x, virtualKeyPosition.y, strokeId));
			++vertexCount;
			currentGestureLineRenderer.positionCount = vertexCount;
			currentGestureLineRenderer.SetPosition(vertexCount - 1, Camera.main.ScreenToWorldPoint(new Vector3(virtualKeyPosition.x, virtualKeyPosition.y, 10)));
		}
	}
	public void PointerExit()
	{
		onPanel = false;
	}
	public void PointerEnter()
	{
		onPanel = true;
	}
	public void PointerDown()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (recognized) // if drawing was recognized next press will clear screen and destroy all of line renderers
			{   
				recognized = false;
				strokeId = -1;

				points.Clear();

				foreach (LineRenderer lineRenderer in gestureLinesRenderer)
				{

					lineRenderer.positionCount = 0;
					Destroy(lineRenderer.gameObject);
				}

                gestureLinesRenderer.Clear();
			}

			++strokeId; // new line id 

			Transform tmpGesture = Instantiate(gestureOnScreenPrefab, transform.position, transform.rotation) as Transform; // new LineRenderer
			currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();

			gestureLinesRenderer.Add(currentGestureLineRenderer);

			vertexCount = 0; // reset amount of points of the line
		}
	}


}
