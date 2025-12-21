#include "Map.h"
#include "Logger.h"
#include <fstream>
#include <algorithm>
#include <cmath>
#include <sstream>
#include <vector>
#include <string>
#include <cstdint>

static Vec3 Add(const Vec3& a, const Vec3& b) { return { a.x + b.x, a.y + b.y, a.z + b.z }; }
static Vec3 Sub(const Vec3& a, const Vec3& b) { return { a.x - b.x, a.y - b.y, a.z - b.z }; }
static Vec3 Mul(const Vec3& a, float s) { return { a.x * s, a.y * s, a.z * s }; }

static Vec3 Cross(const Vec3& a, const Vec3& b)
{
    return { a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x };
}

static Quat QuatInverse(const Quat& q)
{
    return { -q.x, -q.y, -q.z, q.w };
}

static Vec3 QuatRotate(const Quat& q, const Vec3& v)
{
    Vec3 qv{ q.x, q.y, q.z };
    Vec3 t = Mul(Cross(qv, v), 2.0f);
    Vec3 v1 = Add(v, Mul(t, q.w));
    Vec3 v2 = Add(v1, Cross(qv, t));
    return v2;
}

static float AbsF(float v) { return v < 0.0f ? -v : v; }

static float ClampF(float v, float a, float b)
{
    if (v < a) return a;
    if (v > b) return b;
    return v;
}

static bool PushOutCircleFromOBB_XZ(Vec3& p, float radius, const OBB& box)
{
    const float PLAYER_HALF_H = 0.9f;
    Vec3 pCenter = p;
    pCenter.y += PLAYER_HALF_H;

    Quat inv = QuatInverse(box.rot);
    Vec3 rel = Sub(pCenter, box.center);
    Vec3 pl = QuatRotate(inv, rel);

    if (AbsF(pl.y) > box.half.y + PLAYER_HALF_H)
        return false;

    float cx = ClampF(pl.x, -box.half.x, box.half.x);
    float cz = ClampF(pl.z, -box.half.z, box.half.z);

    float dx = pl.x - cx;
    float dz = pl.z - cz;

    float r2 = radius * radius;
    float d2 = dx * dx + dz * dz;

    if (d2 >= r2)
        return false;

    if (d2 > 1e-8f)
    {
        float d = std::sqrt(d2);
        float push = (radius - d);
        float nx = dx / d;
        float nz = dz / d;

        pl.x += nx * push;
        pl.z += nz * push;
    }
    else
    {
        float penX = box.half.x - AbsF(pl.x);
        float penZ = box.half.z - AbsF(pl.z);

        if (penX < penZ)
            pl.x = (pl.x >= 0.0f) ? (box.half.x + radius) : -(box.half.x + radius);
        else
            pl.z = (pl.z >= 0.0f) ? (box.half.z + radius) : -(box.half.z + radius);
    }

    Vec3 pw = Add(box.center, QuatRotate(box.rot, pl));
    p.x = pw.x;
    p.z = pw.z;
    return true;
}

Map& Map::Instance()
{
    static Map inst;
    return inst;
}

void Map::SetWorldBounds(float width, float depth)
{
    worldW = width;
    worldD = depth;

    std::ostringstream oss;
    oss << "월드 바운드 설정 width=" << width << " depth=" << depth;
    Logger::Instance().Write(LogTag::SERVERSTART, oss.str());
}

void Map::ClampXZ(Vec3& p) const
{
    float halfW = worldW * 0.5f;
    float halfD = worldD * 0.5f;

    if (p.x < -halfW) p.x = -halfW;
    if (p.x > halfW) p.x = halfW;

    if (p.z < -halfD) p.z = -halfD;
    if (p.z > halfD) p.z = halfD;
}

bool Map::LoadOBBsFromBinary(const std::string& path)
{
    std::ifstream in(path, std::ios::binary);
    if (!in.is_open())
    {
        std::ostringstream oss;
        oss << "OBB 바이너리 열기 실패 path=" << path;
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    int32_t count = 0;
    in.read(reinterpret_cast<char*>(&count), sizeof(count));
    if (count < 0 || count > 200000)
    {
        std::ostringstream oss;
        oss << "OBB 개수 비정상 count=" << count << " path=" << path;
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    std::vector<OBB> tmp;
    tmp.resize((size_t)count);

    in.read(reinterpret_cast<char*>(tmp.data()), sizeof(OBB) * tmp.size());
    if (!in.good())
    {
        std::ostringstream oss;
        oss << "OBB 데이터 읽기 실패 path=" << path << " count=" << count;
        Logger::Instance().Write(LogTag::SYSTEMERROR, oss.str());
        return false;
    }

    int thin = 0;
    int tall = 0;
    float maxHx = 0.0f, maxHy = 0.0f, maxHz = 0.0f;

    for (auto& b : tmp)
    {
        if (b.half.y < 0.2f) thin++;
        if (b.half.y > 0.5f) tall++;

        if (b.half.x > maxHx) maxHx = b.half.x;
        if (b.half.y > maxHy) maxHy = b.half.y;
        if (b.half.z > maxHz) maxHz = b.half.z;
    }

    {
        std::ostringstream oss;
        oss << "OBB 로드 성공 path=" << path
            << " count=" << count
            << " thinY=" << thin
            << " tallY=" << tall
            << " maxHalf=(" << maxHx << "," << maxHy << "," << maxHz << ")";
        Logger::Instance().Write(LogTag::SERVERSTART, oss.str());
    }

    std::lock_guard<std::mutex> lock(mtx_);
    boxes_.swap(tmp);
    return true;
}

bool Map::RayVsAABB_Local(const Vec3& o, const Vec3& d, const Vec3& mn, const Vec3& mx, float& outT) const
{
    float tmin = 0.0f;
    float tmax = 1e30f;

    auto slab = [&](float ro, float rd, float a, float b) -> bool
        {
            if (std::fabs(rd) < 1e-8f)
                return (ro >= a && ro <= b);

            float inv = 1.0f / rd;
            float t1 = (a - ro) * inv;
            float t2 = (b - ro) * inv;

            if (t1 > t2) std::swap(t1, t2);
            if (t1 > tmin) tmin = t1;
            if (t2 < tmax) tmax = t2;
            return tmin <= tmax;
        };

    if (!slab(o.x, d.x, mn.x, mx.x)) return false;
    if (!slab(o.y, d.y, mn.y, mx.y)) return false;
    if (!slab(o.z, d.z, mn.z, mx.z)) return false;

    outT = tmin;
    return true;
}

bool Map::RayVsOBB(const Vec3& oWorld, const Vec3& dWorld, const OBB& box, float& outT) const
{
    Quat inv = QuatInverse(box.rot);

    Vec3 o = Sub(oWorld, box.center);
    Vec3 oL = QuatRotate(inv, o);
    Vec3 dL = QuatRotate(inv, dWorld);

    Vec3 mn{ -box.half.x, -box.half.y, -box.half.z };
    Vec3 mx{ box.half.x,  box.half.y,  box.half.z };

    return RayVsAABB_Local(oL, dL, mn, mx, outT);
}

RayHit Map::RaycastOBB(const Vec3& origin, const Vec3& dir, float maxDist) const
{
    RayHit best{};
    best.hit = false;
    best.t = maxDist;
    best.point = { origin.x + dir.x * maxDist, origin.y + dir.y * maxDist, origin.z + dir.z * maxDist };

    std::lock_guard<std::mutex> lock(mtx_);
    for (const auto& b : boxes_)
    {
        float t = 0.0f;
        if (RayVsOBB(origin, dir, b, t))
        {
            if (t >= 0.0f && t < best.t)
            {
                best.hit = true;
                best.t = t;
                best.point = { origin.x + dir.x * t, origin.y + dir.y * t, origin.z + dir.z * t };
            }
        }
    }
    return best;
}

void Map::ResolveCircleXZ(Vec3& p, float radius) const
{
    std::lock_guard<std::mutex> lock(mtx_);

    for (int it = 0; it < 4; ++it)
    {
        bool moved = false;
        for (const auto& b : boxes_)
            moved |= PushOutCircleFromOBB_XZ(p, radius, b);

        if (!moved) break;
    }
}