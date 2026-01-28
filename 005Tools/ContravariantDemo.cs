namespace _005Tools
{
    public class ContravariantDemo
    {
        public static void TestContravariance()
        {
            // 1. 创建Dog类型的列表（子类对象列表）
            var dogList = new List<Dog>
                {
                    new Dog("旺财", 3, "中华田园犬"),
                    new Dog("小白", 1, "贵宾犬"),
                    new Dog("大黄", 5, "金毛")
                };

            // 2. 创建AnimalAgeComparer实例（比较器，父类类型）
            IAnimalComparer<Animal> animalComparer = new AnimalAgeComparer();

            // 3. 逆变核心：将 IAnimalComparer<Animal> 隐式转换为 IAnimalComparer<Dog>
            //    （父类型泛型接口 → 子类型泛型接口，符合逆变逻辑，编译通过）
            IAnimalComparer<Dog> dogComparer = animalComparer;

            // 4. 使用比较器对Dog列表进行排序
            dogList.Sort((d1, d2) => dogComparer.Compare(d1, d2));

            // 5. 输出排序结果（按年龄升序）
            Console.WriteLine("按狗的年龄升序排序结果：");
            foreach (var dog in dogList)
            {
                Console.WriteLine($"姓名：{dog.Name}，年龄：{dog.Age}，品种：{dog.Breed}");
            }

            // 经典的Action 逆变示例
            // 父类型委托：接收Animal参数
            Action<Animal> printAnimalName = animal => Console.WriteLine($"动物名称：{animal.Name}");

            // 逆变转换：父类型委托 → 子类型委托
            Action<Dog> printDogName = printAnimalName;

            // 调用子类型委托，传入Dog对象
            printDogName(new Dog("田卡!!!", 3, "中华田园犬")); // 输出：动物名称：旺财
        }
    }
    /// <summary>
    /// 父类:通用动物
    /// </summary>
    public class Animal
    {
        public string? Name { get; set; }

        public int? Age { get; set; }

        public Animal(string? name, int? age)
        {
            Name = name;
            Age = age;
        }
    }
    /// <summary>
    /// 子类: 狗 （继承自Animal）
    /// </summary>
    public class Dog : Animal
    {
        // 狗的特有属性
        public string? Breed { get; set; }

        public Dog(string? name, int? age, string? breed) : base(name, age)
        {
            Breed = breed;
        }
    }
    /// <summary>
    /// 比较器接口（逆变）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAnimalComparer<in T>
    {
        int Compare(T a1, T a2);
    }

    public class AnimalAgeComparer : IAnimalComparer<Animal>
    {
        public int Compare(Animal a1, Animal a2)
        {
            if (a1 == null || a2 == null)
                return 0;
            if (a1 == null)
                return -1;
            if (a2 == null)
                return 1;
            // 比较年龄
            return a1.Age.GetValueOrDefault().CompareTo(a2.Age.GetValueOrDefault());
        }
    }
}
