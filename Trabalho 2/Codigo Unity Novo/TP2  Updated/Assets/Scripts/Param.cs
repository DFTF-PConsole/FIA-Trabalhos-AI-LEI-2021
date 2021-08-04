using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Param : MonoBehaviour
{
    //
    [Header("Imprimir Meu Log")]
    public bool isLogActivo = false;
    //
    [Header("Tempo Maximo Simulacao")]
    public int tempoMaximoSimulacao = 20;
    //
    [Header("MutateGaussian")]
    public float mean = 0.0f;      // mean / median
    public float stdev = 0.5f;     // Standard deviation
    //
    [Header("Versao Funcao Aptidao (1 a 3)")]
    public int funcaoAptidaoRed = 3;
    public int funcaoAptidaoBlue = 3;
    //
    [Header("Pesos Fitness")]                           // 0 = 0
    public float peso1NadaImportante = 1.0f;            // 1
    public float peso2PoucoImportante = 2.0f;           // 2
    public float peso3Importante = 5.0f;                // 3
    public float peso4MuitoImportante = 10.0f;          // 4
    public float peso5MegaImportante = 25.0f;           // 5
    //
    [Header("Associar Peso (0 a 5)")]
    public int distanceToBallPeso = 3;
    public int distanceToMyGoalPeso = 2;
    public int distanceToAdversaryGoalPeso = 1;
    public int distanceToAdversaryPeso = 0;
    public int distancefromBallToMyGoalPeso = 2;
    public int distancefromBallToAdversaryGoalPeso = 3;
    public int distanceToClosestWallPeso = 0;
    public int agentSpeedPeso = 4;
    public int ballSpeedPeso = 3;
    public int advSpeedPeso = 0;
    public int distanceTravelledPeso = 2;
    public int hitTheBallPeso = 4;
    public int hitTheWallPeso = 1;
    public int GoalsOnMyGoalPeso = 5;
    public int GoalsOnAdversaryGoalPeso = 5;
    //
    [Header("")]
    public bool isAdvAtivo = false;                 // evitar usar valores negativos
    public float normalizarValores = 100.0f;        // 0 a 100
    //

    private static Param staticInstance = null;

    public static Param instance
    {
        get
        {
            if (staticInstance == null)
            {
                staticInstance = FindObjectOfType(typeof(Param)) as Param;
            }

            if (staticInstance == null)
            {
                var obj = new GameObject("Param");
                staticInstance = obj.AddComponent<Param>();
            }

            return staticInstance;
        }
    }

    void OnApplicationQuit()
    {
        staticInstance = null;
    }
}
