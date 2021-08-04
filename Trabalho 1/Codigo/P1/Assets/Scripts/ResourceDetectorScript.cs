using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceDetectorScript : MonoBehaviour
{

    public float angleOfSensors = 10f;
    public float rangeOfSensors = 0.1f;
    protected Vector3 initialTransformUp;
    protected Vector3 initialTransformFwd;
    private float strength;
    private float angle;
    public int numObjects;
    public bool debug_mode;
    // Start is called before the first frame update
    void Start()
    {

        initialTransformUp = this.transform.up;
        initialTransformFwd = this.transform.forward;
    }

    // FixedUpdate is called at fixed intervals of time
    void FixedUpdate()
    {
        ObjectInfo anObject;
        anObject = GetClosestPickup();
        if (anObject != null)
        {
            angle = anObject.angle;
            strength = 1.0f / (anObject.distance + 1.0f);
        }
        else
        { // no object detected
            strength = 0;
            angle = 0;
        }
        
    }

    public float GetAngleToClosestResource()
    {
        return angle;
    }


    /**
     *  Funcao de Ativacao Linear para os Recursos
     */
    public float GetLinearOuputRes(float limiarMinX, float limiarMaxX)
    {
        // funcao
        float y = strength;

        //verifica limiares
        if (y <= limiarMinX || y >= limiarMaxX)
            y = 0f;

        return y;
    }


    /**
     *  Funcao de Ativacao Gaussiana para os Recursos
     */
    public virtual float GetGaussianOutputRes(float limiarMinX, float limiarMaxX, float gaussMedia, float gaussDesvioPadrao)
    {
        // funcao
        float y = (float)(1 / (gaussDesvioPadrao * Math.Sqrt(2 * Math.PI)) * Math.Pow(Math.E, -1 * ((Math.Pow(strength - gaussMedia, 2)) / (2 * Math.Pow(gaussDesvioPadrao, 2)))));

        // verificar limiares
        if (strength <= limiarMinX || strength >= limiarMaxX)
            y = 0f;

        return y;
    }


    /**
     *  Funcao de Ativacao Logaritmica para os Recursos
     */
    public virtual float GetLogaritmicOutputRes(float limiarMinX, float limiarMaxX, float logBase)
    {
        // funcao
        float y = -1*(float)Math.Log(strength, logBase);

        // verificar limiares
        if (strength <= limiarMinX || strength >= limiarMaxX)
            y = 0f;

        return y;
    }


    public ObjectInfo[] GetVisiblePickups()
    {
        return (ObjectInfo[]) GetVisibleObjects("Pickup").ToArray();
    }


    public ObjectInfo GetClosestPickup()
    {
        ObjectInfo [] a = (ObjectInfo[])GetVisibleObjects("Pickup").ToArray();
        if(a.Length == 0)
        {
            return null;
        }
        return a[a.Length-1];
    }


    public List<ObjectInfo> GetVisibleObjects(string objectTag)
    {
        RaycastHit hit;
        List<ObjectInfo> objectsInformation = new List<ObjectInfo>();

        for (int i = 0; i * angleOfSensors < 360f; i++)
        {
            if (Physics.Raycast(this.transform.position, Quaternion.AngleAxis(-angleOfSensors * i, initialTransformUp) * initialTransformFwd, out hit, rangeOfSensors))
            {

                if (hit.transform.gameObject.CompareTag(objectTag))
                {
                    if (debug_mode)
                    {
                        Debug.DrawRay(this.transform.position, Quaternion.AngleAxis((-angleOfSensors * i), initialTransformUp) * initialTransformFwd * hit.distance, Color.red);
                    }
                    ObjectInfo info = new ObjectInfo(hit.distance, angleOfSensors * i + 90);
                    objectsInformation.Add(info);
                }
            }
        }

        objectsInformation.Sort();

        return objectsInformation;
    }


    private void LateUpdate()
    {
        this.transform.rotation = Quaternion.Euler(0.0f, 0.0f, this.transform.parent.rotation.z * -1.0f);

    }
}
