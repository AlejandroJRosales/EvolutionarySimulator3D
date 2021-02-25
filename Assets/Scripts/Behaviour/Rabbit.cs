using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rabbit : LivingEntity {
    public float Consume () {
        // float amountConsumed = 10;
        Die(CauseOfDeath.Eaten);

        return 10;
    }
}