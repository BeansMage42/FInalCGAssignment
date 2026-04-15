using UnityEngine;
using Unity.Mathematics;
using System.Collections;

public class LightningController : MonoBehaviour
{
    [SerializeField] private Gradient lightningFlashGradient;
    private Material skyboxMat;
    [SerializeField] private float lightning1Frequency;
    [SerializeField] private float lightning2Frequency;
    [SerializeField] private float lightningSpeed;
    private float countUp;
    private float intensity1;
    private float intensity2;
    [SerializeField] private Light lightningSource1;
    [SerializeField] private Light lightningSource2;
    private Coroutine lightning1 = null;
    private Coroutine lightning2 = null;
    public bool useSkybox = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (useSkybox)
        {
        skyboxMat = RenderSettings.skybox;

        }
        else
        {
            skyboxMat = GetComponent<Renderer>().material;
        }
            lightning1 = StartCoroutine(PlayFlash(lightningSource1, 1));
    }

    // Update is called once per frame
    void Update()
    {
        countUp += Time.deltaTime;
        intensity1 = countUp*lightning1Frequency;
        intensity2 = countUp*lightning2Frequency;

        if (math.frac(intensity1) >= 0.9f && lightning1 == null) lightning1 = StartCoroutine(PlayFlash(lightningSource1, 1)); 
        if (math.frac(intensity2) >= 0.9f && lightning2 == null) lightning2 = StartCoroutine(PlayFlash(lightningSource2, 2)); 
    }

    private IEnumerator PlayFlash(Light lightToAnimate, int lightningIndex)
    {
        if(lightningIndex == 1)
        {
            skyboxMat.SetFloat("_FlashTime1", intensity1);
            
        }
        else
        {
           
                skyboxMat.SetFloat("_FlashTime2", intensity2);
            
        }
            Debug.Log("start animating");
        float uptick = 0;
        string name = (lightningIndex == 1) ? "_Flash1Colour" : "_Flash2Colour";
        Color lightningIntensity = lightningFlashGradient.Evaluate(math.frac(uptick));
        while (uptick < 1)
        {
            skyboxMat.SetColor(name, lightningIntensity);
            lightToAnimate.intensity = lightningIntensity.grayscale;
            Debug.Log(name + " " + lightningIntensity.grayscale);
            yield return null;
            uptick += Time.deltaTime * lightningSpeed;
            lightningIntensity = lightningFlashGradient.Evaluate(math.frac(uptick));

        }
        if(lightningIndex == 1) lightning1 = null;
        else lightning2 = null;
    }
}
