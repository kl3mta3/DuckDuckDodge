using UnityEngine;

public class DucklingFloat : MonoBehaviour
{
	[Header("Bobbing")]
	[SerializeField] private float amplitude = 0.15f; // how high
	[SerializeField] private float frequency = 1.2f;  // how fast (cycles/sec)
	[SerializeField] private Vector3 axis = Vector3.up; // bob direction (usually up)
	[SerializeField] private bool useLocalSpace = true;
	[SerializeField] private bool randomizePhase = true;

	[Header("Extras (optional)")]
	[SerializeField] private bool spin = true;
	[SerializeField] private float spinSpeed = 35f;     // degrees/sec
	[SerializeField] private bool pulseScale = false;
	[SerializeField] private float scaleAmplitude = 0.05f; // 5% size wiggle
	[SerializeField] private float scaleFrequency = 2f;

	private Vector3 startLocalPos;
	private Vector3 startWorldPos;
	private Vector3 baseScale;
	private float phase;

	private void Awake()
	{
		startLocalPos = transform.localPosition;
		startWorldPos = transform.position;
		baseScale = transform.localScale;
		phase = randomizePhase ? Random.value * Mathf.PI * 2f : 0f;
	}

	private void Update()
	{
		float t = Time.time;
		float bob = Mathf.Sin((t * frequency) + phase) * amplitude;
		Vector3 offset = axis.normalized * bob;

		if (useLocalSpace)
			transform.localPosition = startLocalPos + offset;
		else
			transform.position = startWorldPos + offset;

		if (spin)
			transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

		if (pulseScale)
		{
			float s = 1f + Mathf.Sin((t * scaleFrequency) + phase) * scaleAmplitude;
			transform.localScale = baseScale * s;
		}
	}

	// If the object gets disabled (e.g., pickup consumed), this keeps transforms tidy in Editor
	private void OnDisable()
	{
		if (useLocalSpace) transform.localPosition = startLocalPos;
		else               transform.position     = startWorldPos;
		transform.localScale = baseScale;
	}
}