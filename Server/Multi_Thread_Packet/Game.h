#pragma once

enum Direction
{
	UP = 0,
	DOWN,
	LEFT,
	RIGHT
};


class Game
{

public:
	Game()
	{
	}
	//}
	////�÷��̾� ������ ���� �Լ�
	//void SetPlayerMovement(PACKET* pack);

	//// �÷��̾ ���� �Ѵ�.
	//void SpawnPlayers();


	//void ClientSession::SetPlayerMovement(PACKET* pack)
	//{
	//	PACKET_C_MOVE* packet_c_move = (PACKET_C_MOVE*)pack;
	//	mConnected = true;

	//	std::string message = "Dir Input : ";
	//	for (int i = 0; i < 4; i++) //sizeof(ps->Directions)
	//	{
	//		message += (packet_c_move->Directions & (1 << i)) == 1 ? "true " : "false ";
	//	}
	//	cout << message << endl;
	//	// cout << "Name : " << ps->Name << endl << "  recv message : " << ps->Data << "  IP = " << inet_ntoa(mClientAddr.sin_addr) << ",  Port = " << ntohs(mClientAddr.sin_port) << endl;

	//	float movementSize = 0.05f;
	//	// ����Ű�� ������ ���
	//	 // �� ����(0�� ��Ʈ)�� ���ȴ��� Ȯ��
	//	if (packet_c_move->Directions & (1 << UP))
	//		pos.z += movementSize;

	//	// �� ����(1�� ��Ʈ)�� ���ȴ��� Ȯ��
	//	if (packet_c_move->Directions & (1 << DOWN))
	//		pos.z -= movementSize;

	//	// �� ����(2�� ��Ʈ)�� ���ȴ��� Ȯ��
	//	if (packet_c_move->Directions & (1 << LEFT))
	//		pos.x -= movementSize;

	//	// �� ����(3�� ��Ʈ)�� ���ȴ��� Ȯ��
	//	if (packet_c_move->Directions & (1 << RIGHT))
	//		pos.x += movementSize;

	//	PACKET_S_MOVE packet_s_move;
	//	packet_s_move.Id = id;

	//	packet_s_move.Pos = pos;

	//	sessionManager->Broadcast(&packet_s_move);
	//}
};

