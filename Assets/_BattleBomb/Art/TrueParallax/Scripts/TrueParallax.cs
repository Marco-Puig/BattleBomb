using UnityEngine;

/// <summary>
/// Adds parallax effect by using device orientation.
/// </summary>
public class TrueParallax : MonoBehaviour
{
	/// <summary>
	/// Speed of the object.
	/// </summary>
	/// <remarks>When speed increase, object appears to be closer to viewer.</remarks>
	public float Speed;
	
	/// <summary>
	/// Max offset relative to the default position.
	/// </summary>
	public Vector3 MaxOffset = new Vector3(0.5f, 0.5f, 0);
	
	/// <summary>
	/// Whether gyro is enabled.
	/// </summary>
	public bool IsGyroEnabled { get; private set; }
	
	/// <summary>
	/// Use gyro if available.
	/// </summary>
	public static bool UseGyroscope = true;
	
	/// <summary>
	/// Value to smooth a gyro/accelerometer data.
	/// </summary>
	private const float _lerpFactor = 0.4f;
	
	private Vector3 _defaultAcceleration;
	
	private Vector3 _maxPosition;
	
	private Vector3 _minPosition;
	
	private Vector3 _defaultPostion;
	
	void Start()
	{
		if (UseGyroscope && SystemInfo.supportsGyroscope)
		{
			Input.gyro.enabled = true;
			IsGyroEnabled = true;
		}
		
		_defaultAcceleration = IsGyroEnabled ? Input.gyro.gravity : Input.acceleration;
		
		_maxPosition = transform.position + MaxOffset;
		_minPosition = transform.position - MaxOffset;
		_defaultPostion = transform.position;
	}
	
	void Update()
	{
		var acceleration = IsGyroEnabled ? Input.gyro.gravity : Input.acceleration;
		
		// Get new objects position in accordance with the sensor.
		var offset = new Vector3(Speed * (_defaultAcceleration.x - acceleration.x), Speed * (_defaultAcceleration.y - acceleration.y), 0);
		
		var position = _defaultPostion - offset;
		var lerpPosition = Vector3.Lerp (gameObject.transform.position, position, _lerpFactor);
		
		var newPosition = new Vector3(Mathf.Clamp(lerpPosition.x, _minPosition.x, _maxPosition.x),
		                              Mathf.Clamp(lerpPosition.y, _minPosition.y, _maxPosition.y), 
		                              lerpPosition.z);
		
		// Flatten the value obtained with the previous position and set the object to a new position.
		gameObject.transform.position = newPosition;
	}
	
	#if !(UNITY_ANDROID || UNITY_IPHONE)
	void OnGUI()
	{
		GUI.Label(new Rect(0, 0, 300, 20), "TrueParallax only works on iOS and Android platforms.");
	}
	
	#endif
	
}