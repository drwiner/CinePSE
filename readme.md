# AI Film Director
## An automated planning project for narrative cinematography in the Unity 3D environment
###### As part of Dissertation project for David R. Winer


###### This project consists of 5 main stages:
- Preprocessing
	1. Create the virtual world, including the agents, action schemata, locations (as game objects), and a planning problem
	2. Generate the camera candidates, typically automatically created using permutations of cinematography features
- Main Stages
	3. **Compile Hierarchical Task Networks (HTNs)** from *timeline decompositions* created by the user. These represent normative film grammar (e.g. continuity editing) or specific film-related communicative idioms
	4. **Generate Plan** given planning problem and HTNs, produces a hierarchical plan consisting of character actions and camera patterns that conform to the HTNs.
	5. **Execute Plan** from the generated plans. The plans are split into two timelines: a storyworld chronology and a camera schedule. The resulting film is then observable when the timelines are played.


For additional questions...
drwiner at cs.utah.eduh	

Paper coming soon...