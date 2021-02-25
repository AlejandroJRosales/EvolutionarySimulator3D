using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Animal : LivingEntity {

    /*
     * TODO: 
     * Implement SensePotentialMates
     * Disable animals being on top of eachother possibly using walkableCoords from Environment class
     */

    public const int maxViewDistance = 10;

    [EnumFlags]
    public Species diet;

    public CreatureAction currentAction;
    public Genes genes;
    public Color maleColour;
    public Color femaleColour;

    // Settings:
    float timeBetweenActionChoices = 1;
    float moveSpeed = 2f;
    float timeToDeathByHunger = 200;
    float timeToDeathByThirst = 200;

    float drinkDuration = 6;
    float eatDuration = 10;

    float criticalPercent = 0.7f;

    // Visual settings:
    float moveArcHeight = .2f;

    // State:
    [Header ("State")]
    public float hunger;
    public float thirst;

    protected LivingEntity foodTarget;
    protected LivingEntity predatorTarget;
    protected Coord waterTarget;

    // Move data:
    bool animatingMovement;
    // Point A
    Coord moveFromCoord;
    // Point B
    Coord moveTargetCoord;
    Vector3 moveStartPos;
    Vector3 moveTargetPos;
    float moveTime;
    float moveSpeedFactor;
    float moveArcHeightFactor;
    // The path from tile to tile
    Coord[] path;
    int pathIndex;

    // Other
    float lastActionChooseTime;
    const float sqrtTwo = 1.4142f;
    const float oneOverSqrtTwo = 1 / sqrtTwo;

    /**
     * Init initializes the attributes when creating the object
     * 
     * @param coord is the cordinate of the creature when created
     */
    public override void Init (Coord coord) {
        base.Init (coord);
        moveFromCoord = coord;
        // Create random genes
        genes = Genes.RandomGenes (1);
        // This decides the color of the aniamal based on their gender
        material.color = (genes.isMale) ? maleColour : femaleColour;

        // Start the chain by calling, ChooseNextAction function
        ChooseNextAction ();
    }


    /**
     * Update updates
     */
    protected virtual void Update () {

        // Updated the hunger and thirst over with each iteration
        hunger += Time.deltaTime * 1 / timeToDeathByHunger;
        thirst += Time.deltaTime * 1 / timeToDeathByThirst;

        // Animate movement. After moving a single tile, the animal will be able to choose its next action
        if (animatingMovement) {
            AnimateMove ();
        } else {
            // Handle interactions with external things, like food, water, mates
            HandleInteractions ();
            float timeSinceLastActionChoice = Time.time - lastActionChooseTime;
            // If they have gone the set amount of time without interactions then make the next interaction
            if (timeSinceLastActionChoice > timeBetweenActionChoices) {
                ChooseNextAction ();
            }
        }

        if (hunger >= 1) {
            Die (CauseOfDeath.Hunger);
        } else if (thirst >= 1) {
            Die (CauseOfDeath.Thirst);
        }
    }

    /**
     * ChooseNextAction chooses the animals next action after each movement step (1 tile),
     * or, when not moving (e.g interacting with food etc), at a fixed time interval
     */
    protected virtual void ChooseNextAction () {
        // Update the last action time
        lastActionChooseTime = Time.time;
        
        // Get info about surroundings

        // Decide next action:
        // Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
        bool currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
        // Avoiding the predator takes presedence
        if (Environment.SensePredator(moveFromCoord, this, PreferencePenalty))
        {
            AvoidPredator ();
        }
        else if (hunger >= thirst || currentlyEating && thirst < criticalPercent) {
            FindFood ();
        }
        // More thirsty than hungry
        else {
            FindWater ();
        }

        Act ();
    }

    /**
     * AvoidPredator is when the animal is avoiding a predator
     */
    protected virtual void AvoidPredator()
    {
        // Find the food source, then store it in food source
        // It is type LivingEntity because we may add more than one predator later
        // Send SensePredator the animals current coordinates so it can discover a path to run
        LivingEntity predatorSource = Environment.SensePredator(coord, this, PreferencePenalty);

        // Now, if there is a predator source, then set that as the target for movement, else, roam randomly
        if (predatorSource)
        {
            // Set current action to going to avoiding predaotr
            currentAction = CreatureAction.AvoidingPredator;
            // Debug.Log(currentAction);
            predatorTarget = predatorSource;
            // Make a path to food source
            // Since food source is type LivingEntity, it has coordinates to go to
            AvoidanceMove(predatorTarget.coord);
        }
        else
        {
            // set current action to exploring
            currentAction = CreatureAction.Exploring;
        }
    }

    /**
     * FindFood is when the animal is looking for food around itself
     */
    protected virtual void FindFood()
    {
        // Find the food source, then store it in food source
        // It is type LivingEntity because it could be a plant or animal
        // Send SenseFood the animals current coordinates so it can discover a path
        LivingEntity foodSource = Environment.SenseFood(coord, this, PreferencePenalty);

        // Now, if there is a food source, then set that as the target for movement, else, roam randomly
        if (foodSource)
        {
            // Set current action to going to food
            currentAction = CreatureAction.GoingToFood;
            foodTarget = foodSource;
            // Make a path to food source
            // Since food source is type LivingEntity, it has coordinates to go to
            CreatePath(foodTarget.coord);

        }
        else
        {
            // set current action to exploring
            currentAction = CreatureAction.Exploring;
        }
    }

    /**
     * FindFood is when the animal is looking for water around itself
     */
    protected virtual void FindWater () {
        // Find the water source, then store it
        // Send FindWater the animals current coordinates so it can discover a path
        Coord waterTile = Environment.SenseWater (coord);
        
        // If the waterTile coordinate is valid then go to the water, else explore
        if (waterTile != Coord.invalid) {
            // Set creature action to GoingToWater
            currentAction = CreatureAction.GoingToWater;
            waterTarget = waterTile;
            // Make a path to water source
            // The waterTarget has coordinates
            CreatePath(waterTarget);

        } else {
            currentAction = CreatureAction.Exploring;
        }
    }

    /**
     * FoodPrefencePenalty When choosing from multiple food sources, the one with the lowest penalty will be selected
     */
    protected virtual int PreferencePenalty (LivingEntity self, LivingEntity food) {
        return Coord.SqrDistance (self.coord, food.coord);
    }

    /**
     * Based on the currentAction move there
     */
    protected void Act () {
        //Debug.Log(currentAction);
        switch (currentAction) {
            // If exploring, chose the next tile by weight, with those coordinates and move to coordinates
            case CreatureAction.Exploring:
                StartMoveToCoord (Environment.GetNextTileWeighted (coord, moveFromCoord));
                break;
            case CreatureAction.AvoidingPredator:
                AvoidanceMove (predatorTarget.coord);
                break;
            case CreatureAction.GoingToFood:
                // Detect if current coordinates of the animal is neighboor to foodTartget.coord
                // If so then change CreatureAction from going to food to eating
                if (Coord.AreNeighbours (coord, foodTarget.coord)) {
                    LookAt (foodTarget.coord);
                    // Then set current action to eating
                    currentAction = CreatureAction.Eating;
                } else {
                    // If they are not neighbors then start the treck there given the pa TODO
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.GoingToWater:
                // Detect if the coordinates of the animal is neighbored to waterTarget coordinates
                // If so then change CreatureAction from going to water to drinking
                if (Coord.AreNeighbours (coord, waterTarget)) {
                    LookAt (waterTarget);
                    // Set current action to drinking
                    currentAction = CreatureAction.Drinking;
                } else {
                    // If they are not neighbors then start the treck there given the pa TODO
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                }
                break;
        }
    }

    /**
     * AvoidanceMove determines a point for the prey to run away to to avoid the predator chasing it
     */
    protected void AvoidanceMove (Coord target)
    {
        target.x = coord.x + Math.Sign(coord.x - target.x);
        target.y = coord.y + Math.Sign(coord.y - target.y);

        //int worldSize = Environment.walkable.Length - 1;
        //// Check if where the animal wants to move is not outside the map
        //if (target.x < worldSize && target.y < worldSize)

        // If the spot they want to move is walkable
        if (Environment.walkable[target.x, target.y])
        {
            StartMoveToCoord(target);
        }
        else
        {
            // If where they want to move is not inside the map or not walkable, ie water, etc, then roam
            StartMoveToCoord(Environment.GetNextTileWeighted(coord, moveFromCoord));
        }
    }

    /**
     * CreatePath Get the path from current to target TODO
     */
    protected void CreatePath (Coord target) {
        // Create new path if current is not already going to target
        if (path == null || pathIndex >= path.Length || (path[path.Length - 1] != target || path[pathIndex - 1] != moveTargetCoord)) {
            path = EnvironmentUtility.GetPath (coord.x, coord.y, target.x, target.y);
            pathIndex = 0;
        }
    }

    /**
     * StartMoveToCoord starts the move to the target TODO
     */
    protected void StartMoveToCoord (Coord target) {
        moveFromCoord = coord;
        moveTargetCoord = target;
        moveStartPos = transform.position;
        moveTargetPos = Environment.tileCentres[moveTargetCoord.x, moveTargetCoord.y];
        animatingMovement = true;

        bool diagonalMove = Coord.SqrDistance (moveFromCoord, moveTargetCoord) > 1;
        moveArcHeightFactor = (diagonalMove) ? sqrtTwo : 1;
        moveSpeedFactor = (diagonalMove) ? oneOverSqrtTwo : 1;

        LookAt (moveTargetCoord);
    }

    /**
     * LookAt gets the distance between target and current animal position
     */
    protected void LookAt (Coord target) {
        if (target != coord) {
            Coord offset = target - coord;
            transform.eulerAngles = Vector3.up * Mathf.Atan2 (offset.x, offset.y) * Mathf.Rad2Deg;
        }
    }

    /**
     * HandleInteractions takes care of the animal eating and drinking
     */
    void HandleInteractions () {
        // If the the current action is eating the first check if the their is a food target? and the animal is hungry then eat TODO
        if (currentAction == CreatureAction.Eating) {
            // If foodTarget is not null and the animal is hungry
            if (foodTarget && hunger > 0) {
                float eatAmount = 0;
                // If the food is type plant then access the consume function from Plant class
                if (foodTarget is Plant)
                {
                    eatAmount = Mathf.Min(hunger, Time.deltaTime * 1 / eatDuration);
                    eatAmount = ((Plant)foodTarget).Consume(eatAmount);
                }
                // Else if the food is type animal then access the cosume function from Animal class
                else if (foodTarget is Animal)
                {
                    eatAmount = ((Animal)foodTarget).Consume();
                }
                hunger -= eatAmount;
            }
        // If the current action is drinking, then check if the animal is thirsty
        } else if (currentAction == CreatureAction.Drinking) {
            if (thirst > 0)
            {
                // Drink
                thirst -= Time.deltaTime * 1 / drinkDuration;
                thirst = Mathf.Clamp01(thirst);
            }
        }
    }

    /**
     * Cosnume allows the animal to be eaten and returns some amount of food resotration. Temporary amount for now
     */
    public float Consume()
    {
        Die(CauseOfDeath.Eaten);
        return (float)(hunger * 0.5);
    }

    void AnimateMove () {
        // Move in an arc from start to end tile
        moveTime = Mathf.Min (1, moveTime + Time.deltaTime * moveSpeed * moveSpeedFactor);
        float height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight * moveArcHeightFactor;
        transform.position = Vector3.Lerp (moveStartPos, moveTargetPos, moveTime) + Vector3.up * height;

        // Finished moving
        if (moveTime >= 1) {
            // Environment.RegisterMove (this, coord, moveTargetCoord);
            Environment.RegisterMove(this, moveFromCoord, moveTargetCoord);
            coord = moveTargetCoord;

            animatingMovement = false;
            moveTime = 0;
            ChooseNextAction ();
        }
    }

    void OnDrawGizmosSelected () {
        if (Application.isPlaying) {
            var surroundings = Environment.Sense (coord);
            Gizmos.color = Color.white;
            if (surroundings.nearestFoodSource != null) {
                Gizmos.DrawLine (transform.position, surroundings.nearestFoodSource.transform.position);
            }
            if (surroundings.nearestWaterTile != Coord.invalid) {
                Gizmos.DrawLine (transform.position, Environment.tileCentres[surroundings.nearestWaterTile.x, surroundings.nearestWaterTile.y]);
            }

            if (currentAction == CreatureAction.GoingToFood) {
                var path = EnvironmentUtility.GetPath (coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y);
                Gizmos.color = Color.black;
                for (int i = 0; i < path.Length; i++) {
                    Gizmos.DrawSphere (Environment.tileCentres[path[i].x, path[i].y], .2f);
                }
            }
        }
    }

}