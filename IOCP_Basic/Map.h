#pragma once
#include <vector>
#include <mutex>
#include <string>

#pragma pack(push, 1)
struct Vec3
{
    float x, y, z;
};

struct Quat
{
    float x, y, z, w;
};

struct OBB
{
    Vec3 center;
    Vec3 half;
    Quat rot;
};
#pragma pack(pop)

struct RayHit
{
    bool hit;
    float t;
    Vec3 point;
};

class Map
{
public:
    static Map& Instance();

    bool LoadOBBsFromBinary(const std::string& path);

    void SetWorldBounds(float width, float depth);
    void ClampXZ(Vec3& p) const;

    RayHit RaycastOBB(const Vec3& origin, const Vec3& dir, float maxDist) const;
    void ResolveCircleXZ(Vec3& p, float radius) const;

private:
    Map() = default;

    bool RayVsOBB(const Vec3& oWorld, const Vec3& dWorld, const OBB& box, float& outT) const;
    bool RayVsAABB_Local(const Vec3& o, const Vec3& d, const Vec3& mn, const Vec3& mx, float& outT) const;

private:
    float worldW = 60.0f;
    float worldD = 60.0f;

    std::vector<OBB> boxes_;
    mutable std::mutex mtx_;
};