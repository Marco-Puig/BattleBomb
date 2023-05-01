using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadCharacter : MonoBehaviour
{
	public GameObject[] characterPrefabs;
	public Transform spawnPoint;
	private Transform target;
	//public TMP_Text label;

	public float distance = -10f;

	public float height = 0f;

	public float damping = 5.0f;

	public float mapX = 100.0f;
	public float mapY = 100.0f;

	private float minX = 0f;
	private float maxX = 0f;
	private float minY = 0f;
	private float maxY = 0f;

	void Start()
	{
		minX = transform.position.x;
		minY = transform.position.y;
		maxX = mapX;
		maxY = mapY;
		int selectedCharacter = PlayerPrefs.GetInt("selectedCharacter");
		GameObject prefab = characterPrefabs[selectedCharacter];
		GameObject clone = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
		target = clone.transform;

		//label.text = prefab.name;
	}
	void Update()
	{
		// get the position of the target (AKA player)
		Vector3 wantedPosition = target.TransformPoint(0, height, distance);

		// check if it's inside the boundaries on the X position
		wantedPosition.x = (wantedPosition.x < minX) ? minX : wantedPosition.x;
		wantedPosition.x = (wantedPosition.x > maxX) ? maxX : wantedPosition.x;

		// check if it's inside the boundaries on the Y position
		wantedPosition.y = (wantedPosition.y < minY) ? minY : wantedPosition.y;
		wantedPosition.y = (wantedPosition.y > maxY) ? maxY : wantedPosition.y;

		// set the camera to go to the wanted position in a certain amount of time
		transform.position = Vector3.Lerp(transform.position, wantedPosition, (Time.deltaTime * damping));
	}
}
