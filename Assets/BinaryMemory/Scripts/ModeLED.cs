using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModeLED : MonoBehaviour {
	public GameObject MeshOn;
	public GameObject MeshOff;

	private bool _on = false;
	public bool On { get { return _on; } set { if (_on == value) return; _on = value; UpdateMeshes(); } }

	private void Start() {
		UpdateMeshes();
	}

	private void UpdateMeshes() {
		MeshOn.SetActive(On);
		MeshOff.SetActive(!On);
	}
}
