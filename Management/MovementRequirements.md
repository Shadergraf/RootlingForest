

# Character Requirements

## Movement

- Slide down steep slopes and prevent movement up those slopes



## Grabbing & Carrying

### General
(Almost) all physics objects should be able to be carried or grabbed in some way. A grab should be simulated by a physics constraint.

### Carrying Archetypes
#### Holding
Occurs if the object is both small/medium & light.
#### Carrying
Occurs if the object is small/medium & heavy
#### Dragging
Occurs if the object is big & heavy
#### Pushing
Occurs if the object is unwieldy


- Update Grab position
   - Objects sliding on the ground should be lifted up to allow for smoother walking up slopes