#pragma once

enum IconType
{
    Cat,
    Giraffe,
    Rabbit,
    Pig,
    Elephant,
    Panda,
    Monkey,
    Goat,
    Sheep,
    Raccoon,
    Lemur,
    Meerkat,
    Hippopotamus,
    Mouse,
    Dog,
    Bear,
    Lion,
    Tiger,
    Fox,
    Wolf
};



class UserIcon
{
public:
    static const char* GetIconName(int index) {
        switch ((IconType)index) {
        case IconType::Cat: return "Cat";
        case IconType::Giraffe: return "Giraffe";
        case IconType::Rabbit: return "Rabbit";
        case IconType::Pig: return "Pig";
        case IconType::Elephant: return "Elephant";
        case IconType::Panda: return "Panda";
        case IconType::Monkey: return "Monkey";
        case IconType::Goat: return "Goat";
        case IconType::Sheep: return "Sheep";
        case IconType::Raccoon: return "Raccoon";
        case IconType::Lemur: return "Lemur";
        case IconType::Meerkat: return "Meerkat";
        case IconType::Hippopotamus: return "Hippopotamus";
        case IconType::Mouse: return "Mouse";
        case IconType::Dog: return "Dog";
        case IconType::Bear: return "Bear";
        case IconType::Lion: return "Lion";
        case IconType::Tiger: return "Tiger";
        case IconType::Fox: return "Fox";
        case IconType::Wolf: return "Wolf";
        default:
            cout << "Invalid Index : " << index << endl;
            return "Unknown";
        }
    }

};

