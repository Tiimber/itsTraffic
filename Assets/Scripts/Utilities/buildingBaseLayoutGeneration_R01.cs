/*
 * Project Name: Procedural Buildings
 * Scene Name: MainScene
 * Version: R0.1
 * Date: May 2013
 * Script Author: Elias Kalapanidas
 * 
 * Objective: Procedural Generation of as-much-as possible realistic buildings, to be used in a procedurally generated city (a destractibe urban environment)
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using proceduralBuildings_R01;

public class buildingBaseLayoutGeneration_R01 : MonoBehaviour {
	public int maximumIterations = 6;
	
	private int currentIteration = 0;
	private RectangularShapeFactory rectangularFactory = new RectangularShapeFactory();
	
	// Use this for initialization
	void Start () {  
		GameObject rectangle = rectangularFactory.createShape();
		ShapeExtrude extrude = new ShapeExtrude();
		extrude.setShapeToTransform(rectangle);
		extrude.execute();
	}
	
	// Update is called once per frame
	void Update () {
		
	} // Update() ends here
	
}; // buildingBaseLayoutGeneration class ends here





// Elementary enumeration set, listing all shape types
public enum ShapeEnum {Rectangle};

// Abstract class that all shape classes should inherit from
public abstract class ShapeFactory {
	protected ShapeEnum _type;
	protected ArrayList _shapes;
	
	public ShapeFactory() { _shapes = new ArrayList(); }
	public ShapeEnum getShapeType() { return _type; }
	public int getShapesCount() { 
		if (_shapes != null) 
			return _shapes.Count; 
		else {
			Debug.LogError("You have called the method ShapeFactory.getShapesCount but the _shapes ArrayList property was null. Make sure that you have properly constructed your instance of ShapeFactory class.");
			return 0;
		}
	}
	public GameObject getShape(int shapeIndex) { 
		if (_shapes == null) {
			Debug.LogError("You have called the method ShapeFactory.getShape but the _shapes ArrayList property was null. Make sure that you have properly constructed your instance of ShapeFactory class.");
			return null;
		}
		if (shapeIndex >= _shapes.Count) {
			Debug.LogError("You have called the method ShapeFactory.getShape but the shapeIndex argument was greater than the _shapes ArrayList size. Make sure that the argument is always lower than the size of the _shapes ArrayList.");
			return null;
		}
		return (GameObject)_shapes[shapeIndex];
	}
	
	public abstract GameObject createShape();
};



//ToDo: introduce a side ratio so that we don't have to create squares all the time
public class RectangularShapeFactory : ShapeFactory {
	override public GameObject createShape() {
		GameObject _object = new GameObject("Rectangle_" + _shapes.Count);
		_object.AddComponent <MeshFilter>();
		_object.AddComponent <MeshRenderer>();
		Mesh myMesh = _object.GetComponent<MeshFilter>().mesh;
		myMesh.vertices = new Vector3[] {new Vector3(-1,0,1), new Vector3(-1,0,-1), new Vector3(1,0,-1), new Vector3(1,0,1)};
		myMesh.normals = new Vector3[] {Vector3.up, Vector3.up, Vector3.up, Vector3.up};
		myMesh.triangles =  new int[] {0,2,1,0,3,2}; //The winding order of triangles controls which side is visible. Clockwise facing = visible, counter-clockwise = invisible.
		myMesh.uv = new Vector2[] {new Vector2(0,1), new Vector2(0,0), new Vector2(1,0), new Vector2(1,1)};
		myMesh.colors = new Color[] {Color.white,Color.white,Color.white,Color.white};
		;
		myMesh.RecalculateBounds();
		myMesh.RecalculateNormals();
		// Add the new _object GameObject to the _shapes array;
		_shapes.Add(_object);
		
		return _object;
	}
};



//Commands enumeration
public enum CommandEnum {create, extrude};

// Abstract class that all command classes should inherit from
public abstract class ShapeCommand {
	protected CommandEnum _class;
	
	abstract public GameObject execute();
	//public abstract bool executeShapeCommand(CommandEnum command); 
};



public class ShapeCreate : ShapeCommand {
	protected ShapeEnum _type = ShapeEnum.Rectangle;
	protected ShapeFactory _factory = null;
	
	public void setShapeType(ShapeEnum shapeType) { _type = shapeType; }
	public void setShapeFactory(ShapeFactory aFactory) {
		if (aFactory != null)
			_factory = aFactory;
		else
			Debug.LogError("ShapeFactory argument in a call to ShapeCreate.setShapeFactory was null. Please provide a properly instantiated value (not-null).");   
	}   
	override public GameObject execute() {
		if (_factory == null) {
			Debug.LogError("ShapeCreate.execute is called but the _factory property is null. Make sure that you have properly instantiated _factory by calling the setShapeFactory method of this class just before calling execute.");
			return null;
		}
		return _factory.createShape();
	}
};

public class ShapeExtrude : ShapeCommand {
	protected GameObject _shape;
	
	public void setShapeToTransform(GameObject aShape) { _shape = aShape; }
	override public GameObject execute() {
		Mesh mesh = _shape.GetComponent<MeshFilter>().mesh;
		Matrix4x4 [] extrusionPath = new Matrix4x4 [2];
		extrusionPath[0] = _shape.transform.worldToLocalMatrix * Matrix4x4.TRS(_shape.transform.position, Quaternion.identity, Vector3.one);
		extrusionPath[1] = _shape.transform.worldToLocalMatrix * Matrix4x4.TRS(_shape.transform.position + new Vector3(0, 1f, 0), Quaternion.identity, Vector3.one);
		MeshExtrusion.ExtrudeMesh(mesh, _shape.GetComponent<MeshFilter>().mesh, extrusionPath, false);
		return _shape;
	}
};

