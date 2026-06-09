using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using RacingSimulator.Models;

namespace RacingSimulator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _dllPath;
        private Type _selectedType;
        private MethodInfo _selectedMethod;
        private string _executionResult;

        public string DllPath
        {
            get => _dllPath;
            set { _dllPath = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Type> FoundTypes { get; set; } = new ObservableCollection<Type>();
        public ObservableCollection<MethodInfo> Methods { get; set; } = new ObservableCollection<MethodInfo>();
        
        // Поля ввода для конструктора класса
        public ObservableCollection<MethodParameterModel> ConstructorParameters { get; set; } = new ObservableCollection<MethodParameterModel>();
        
        // Поля ввода для выбранного метода
        public ObservableCollection<MethodParameterModel> MethodParameters { get; set; } = new ObservableCollection<MethodParameterModel>();

        public Type SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
                OnTypeChanged();
            }
        }

        public MethodInfo SelectedMethod
        {
            get => _selectedMethod;
            set
            {
                _selectedMethod = value;
                OnPropertyChanged();
                OnMethodChanged();
            }
        }

        public string ExecutionResult
        {
            get => _executionResult;
            set { _executionResult = value; OnPropertyChanged(); }
        }

        public ICommand LoadDllCommand { get; }
        public ICommand ExecuteMethodCommand { get; }

        public MainViewModel()
        {
            LoadDllCommand = new RelayCommand(_ => LoadDll());
            ExecuteMethodCommand = new RelayCommand(_ => ExecuteMethod(), _ => SelectedType != null && SelectedMethod != null);
            
            // Предзаполним путь для удобства (измените под себя при необходимости)
            DllPath = Path.Combine(AppDomain.DomainDomain ?? AppContext.BaseDirectory, "RacingExtensions.dll");
        }

        private void LoadDll()
        {
            try
            {
                if (!File.Exists(DllPath))
                {
                    MessageBox.Show("Указанный файл DLL не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                FoundTypes.Clear();
                Methods.Clear();
                ConstructorParameters.Clear();
                MethodParameters.Clear();

                // Загружаем сборку динамически
                Assembly assembly = Assembly.LoadFrom(DllPath);

                // Ищем типы, которые реализуют IRacingService и являются классами
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.GetInterface("IRacingService") != null)
                    .ToList();

                if (!types.Any())
                {
                    MessageBox.Show("В DLL не найдены классы, реализующие интерфейс IRacingService.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var type in types)
                {
                    FoundTypes.Add(type);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке DLL: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnTypeChanged()
        {
            Methods.Clear();
            ConstructorParameters.Clear();
            MethodParameters.Clear();

            if (SelectedType == null) return;

            // Берём первый публичный конструктор для динамической сборки объекта
            var constructor = SelectedType.GetConstructors().FirstOrDefault();
            if (constructor != null)
            {
                foreach (var param in constructor.GetParameters())
                {
                    ConstructorParameters.Add(new MethodParameterModel 
                    { 
                        Name = $"[Конструктор] {param.Name}", 
                        ParameterType = param.ParameterType 
                    });
                }
            }

            // Получаем все публичные методы текущего класса (исключая системные object)
            var methods = SelectedType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName) // исключаем геттеры/сеттеры свойств
                .ToList();

            foreach (var method in methods)
            {
                Methods.Add(method);
            }
        }

        private void OnMethodChanged()
        {
            MethodParameters.Clear();
            if (SelectedMethod == null) return;

            // Формируем список параметров для выбранного метода
            foreach (var param in SelectedMethod.GetParameters())
            {
                MethodParameters.Add(new MethodParameterModel 
                { 
                    Name = param.Name, 
                    ParameterType = param.ParameterType 
                });
            }
        }

        private void ExecuteMethod()
        {
            try
            {
                // 1. Собираем параметры и создаем инстанс класса через рефлексию
                var ctor = SelectedType.GetConstructors().FirstOrDefault();
                if (ctor == null) return;

                object[] ctorArgs = ConvertParameters(ConstructorParameters);
                object instance = Activator.CreateInstance(SelectedType, ctorArgs);

                // 2. Собираем параметры для выполнения метода
                object[] methodArgs = ConvertParameters(MethodParameters);

                // 3. Вызываем метод
                object result = SelectedMethod.Invoke(instance, methodArgs);

                // 4. Выводим результат работы метода
                ExecutionResult = result != null 
                    ? $"Успешно!\nРезультат: {result}" 
                    : "Метод выполнен успешно (тип возвращаемого значения void).";
            }
            catch (TargetInvocationException tie)
            {
                ExecutionResult = $"Ошибка внутри вызванного метода: {tie.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                ExecutionResult = $"Ошибка рефлексии: {ex.Message}";
            }
        }

        // Преобразование строковых значений из TextBox в реальные типы данных параметров
        private object[] ConvertParameters(ObservableCollection<MethodParameterModel> parameters)
        {
            object[] converted = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];
                if (string.IsNullOrEmpty(p.Value))
                {
                    // Дефолтные значения, если пользователь ничего не ввел
                    converted[i] = p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
                }
                else
                {
                    // Приведение типов (String -> Int, String -> Boolean и т.д.)
                    converted[i] = Convert.ChangeType(p.Value, p.ParameterType);
                }
            }
            return converted;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}