using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockDetectorScript : MonoBehaviour
{

    public float angleOfSensors = 5f;
    public float rangeOfSensors = 5f;
    protected Vector3 initialTransformUp;
    protected Vector3 initialTransformFwd;
    private float strength;
    private float angleToClosestObj;
    public int numObjects;
    public bool debugMode;
    // Start is called before the first frame update
    void Start()
    {

        initialTransformUp = this.transform.up;
        initialTransformFwd = this.transform.forward;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ObjectInfo anObject;
        anObject = GetClosestObstacle();
        if (anObject != null)
        {
            angleToClosestObj = anObject.angle;
            strength = 1.0f / (anObject.distance + 1.0f);
        }
        else
        { // no object detected
            strength = 0;
            angleToClosestObj = 0;
        }
    }

    public float GetAngleToClosestObstacle()
    {
        return angleToClosestObj;
    }

    /**
     *  Funcao de Ativacao Linear para os Obstaculos
     */
    public float GetLinearOutputObs(float limiarMinX, float limiarMaxX)
    {
        // funcao
        float y = strength;

        // verificar limiares
        if (y <= limiarMinX || y >= limiarMaxX)
            y = 0f;

        return y;
    }


    public ObjectInfo GetClosestObstacle()
    {
        ObjectInfo[] a = (ObjectInfo[])GetVisibleObstacle("Wall").ToArray();
        if (a.Length == 0)
        {
            return null;
        }
        return a[a.Length - 1];
    }


    public List<ObjectInfo> GetVisibleObstacle(string objectTag)
    {
        RaycastHit hit;
        List<ObjectInfo> objectsInformation = new List<ObjectInfo>();

        for (int i = 0; i * angleOfSensors < 360f; i++)
        {
            if (Physics.Raycast(this.transform.position, Quaternion.AngleAxis(-angleOfSensors * i, initialTransformUp) * initialTransformFwd, out hit, rangeOfSensors))
            {

                if (hit.transform.gameObject.CompareTag(objectTag))
                {
                    if (debugMode)
                    {
                        Debug.DrawRay(this.transform.position, Quaternion.AngleAxis((-angleOfSensors * i), initialTransformUp) * initialTransformFwd * hit.distance, Color.green);
                    }
                    ObjectInfo info = new ObjectInfo(hit.distance, angleOfSensors * i + 90);
                    objectsInformation.Add(info);
                }
            }
        }

        objectsInformation.Sort();

        return objectsInformation;
    }

    /**
     *  Funcao de Ativacao Gaussiana para os Obstaculos
     */
    public virtual float GetGaussianOutputObs(float limiarMinX, float limiarMaxX, float gaussMedia, float gaussDesvioPadrao)
    {
        // funcao
        float y = (float) ((1 * Math.Pow(Math.E, -1 * ((Math.Pow(strength - gaussMedia, 2)) / (2 * Math.Pow(gaussDesvioPadrao, 2))))) /(gaussDesvioPadrao * Math.Sqrt(2*Math.PI)) );

        // verificar limiares
        if (strength <= limiarMinX || strength >= limiarMaxX)
            y = 0f;

        return y;
    }

    /**
     *  Funcao de Ativacao Logaritmica para os Obstaculos
     */
    public virtual float GetLogaritmicOutputObs(float limiarMinX, float limiarMaxX, float logBase)
    {
        // funcao
        float y = -1*(float)Math.Log(strength, logBase);

        // verificar limiares
        if (strength <= limiarMinX || strength >= limiarMaxX)
            y = 0f;

        return y;
    }
}
