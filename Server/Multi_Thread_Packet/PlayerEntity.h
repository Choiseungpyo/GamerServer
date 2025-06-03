#pragma once



class PlayerEntity
{
	int id;
	string name;
	TeamType teamType;
	Vector3 position;
	Vector3 rotation;
	PlayerState state;


	const int maxHp = 30;
	int currHp;

public:
	PlayerEntity(int id, const char* name, TeamType teamType, Vector3 pos)
		:id(id), name(name), teamType(teamType), position(pos), state(PlayerState::IDLE), currHp(maxHp)
	{
		if (teamType == RED)
			rotation = Vector3(0, 180, 0);
		else
			rotation = Vector3();
	}
	~PlayerEntity() {}

	int GetId() const { return id; }
	const string& GetName() const { return name; }
	TeamType GetTeamType() const { return teamType; }
	Vector3 GetPosition() const { return position; }
	Vector3 GetRotation() const { return rotation; }
	PlayerState GetState() const { return state; }
	int GetCurrHp() const { return currHp; }
};

