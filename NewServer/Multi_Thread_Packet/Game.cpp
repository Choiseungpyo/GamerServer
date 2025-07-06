#include "stdafx.h"
#include "Game.h"

void Game::InitTeamSpawnPos()
{
	redTeamSpawnPos[0] = Vector3(6, 0, 13);
	redTeamSpawnPos[1] = Vector3(1.5, 0, 13);
	redTeamSpawnPos[2] = Vector3(-1.5f, 0, 13);
	redTeamSpawnPos[3] = Vector3(-7, 0, 13);

	blueTeamSpawnPos[0] = Vector3(6, 0, -13);
	blueTeamSpawnPos[1] = Vector3(1.5, 0, -13);
	blueTeamSpawnPos[2] = Vector3(-1.5f, 0, -13);
	blueTeamSpawnPos[3] = Vector3(-7, 0, -13);
}

void Game::InitPlayerEntityMap()
{
	auto roomUserInfoMap = room->GetRoomUserInfoMap();
	auto clientMap = room->GetClientMap();

	for (auto& pair : roomUserInfoMap)
	{
		auto it = clientMap.find(pair.first);

		// 못찾았을 경우
		if (it == clientMap.end()) {
			cout << "clientMap - Invalid Id : " << pair.first << endl;
			continue;
		}
		auto roomUserInfo = pair.second;

		auto client = it->second;
		auto user = client->GetUser();

		int order = -1;
		Vector3 pos = Vector3();
		if (roomUserInfo->teamType == RED)
		{
			order = room->GetRedTeamUserOrder(pair.first);
			if (order == -1)
			{
				cout << "RedTeamUserOrder - Invalid Id : " << pair.first;
				continue;
			}
			pos = redTeamSpawnPos[order];
		}
		else if (roomUserInfo->teamType == BLUE)
		{
			order = room->GetBlueTeamUserOrder(pair.first);
			if (order == -1)
			{
				cout << "BlueTeamUserOrder - Invalid Id : " << pair.first;
				continue;
			}
			pos = blueTeamSpawnPos[order];
		}

		playerEntityMap[pair.first] = new PlayerEntity(user->GetId(), user->GetName(), roomUserInfo->teamType, pos);
	}
}

void Game::SpawnAllPlayerEntity()
{
	vector<char> buffer;

	int userCnt = room->GetMatchType() * 2;
	auto header = PACKET_INFO_HEADER(userCnt);
	header.Type = S_C_GAME_SPAWN_ALL;
	header.Length = sizeof(PACKET_INFO_HEADER) + userCnt * sizeof(PACKET_S_C_PLAYERENTITY_DATA);
	buffer.resize(header.Length);
	memcpy(buffer.data(), &header, sizeof(PACKET_INFO_HEADER));

	size_t offset = sizeof(PACKET_INFO_HEADER);
	for (const auto& pair : playerEntityMap)
	{
		auto playerEntity = pair.second;
		PACKET_S_C_PLAYERENTITY_DATA pack;
		pack.userId = playerEntity->GetId();
		strncpy_s(pack.userName, sizeof(pack.userName), playerEntity->GetName().c_str(), _TRUNCATE);
		pack.teamType = playerEntity->GetTeamType();
		pack.state = playerEntity->GetState();
		pack.position = playerEntity->GetPosition();
		pack.Rotataion = playerEntity->GetRotation();
		pack.currHp = playerEntity->GetCurrHp();

		memcpy(buffer.data() + offset, &pack, sizeof(PACKET_S_C_PLAYERENTITY_DATA));
		offset += sizeof(PACKET_S_C_PLAYERENTITY_DATA);
	}

	room->SendToAllUserInRoom(buffer);
}