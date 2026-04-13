using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WaterForceFieldControl : MonoBehaviour
{
    [SerializeField] private float minSize;
    [SerializeField] private float maxSize;
    [SerializeField] private float speed;
    private float amount;
    private float timer;
    private Renderer r;
    private bool forward;
    private bool started;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        r = GetComponent<Renderer>();
    }

    public void StartEffect(bool expand)
    {
        forward = expand;
        started = true;
        timer = 0;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard.gKey.wasPressedThisFrame)StartEffect(true);
        if (keyboard.lKey.wasPressedThisFrame) StartEffect(false);
        if (started) 
        {
            timer += Time.deltaTime * speed;
            if (forward)
            {
                amount = Mathf.Lerp(minSize, maxSize, timer);
                if (amount >= maxSize) started = false;
            }
            else
            {
                amount = Mathf.Lerp (maxSize, minSize, timer);
                if (amount <= minSize) started = false;
                
            }

            foreach (Material m in r.materials) 
            {
                m.SetFloat("_BlobSize",amount);
            }

            
        }
    }

    




}
