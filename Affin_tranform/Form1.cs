using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Affin_tranform
{
    public partial class Form1 : Form
    {
        Bitmap bmp;
        Pen pen = new Pen(Color.Red, 4);
        public Form1()
        {
            InitializeComponent();
            bmp = new Bitmap(pictureBox.Size.Width, pictureBox.Size.Height);
            pictureBox.Image = bmp;
            pen.EndCap = LineCap.ArrowAnchor;
        }

        Graphics g;
        List<my_point> points = new List<my_point>(); // список точек
        List<edge> edges = new List<edge>(); // список рёбер
        my_point select_point_add = null; // для рисования ребра
        my_point select_point_del = null; // для  удаления ребра
        my_point rotate_point = null; // точка, относительно которой поворачиваем
        my_point center_point = null; // центральная точка фигуры
                                      //my_point position_point = null;
        bool is_point_rot = false; // является ли точка центром для поворота
                                   //bool is_point_pos = false; // является ли точка проверяемой на расположение относительно ребра
        SolidBrush brush = new SolidBrush(Color.LightGreen);

        /// <summary>
        /// Добавляем/удаляем точки и рёбра
        /// </summary>
        private void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            my_point p = null;
            foreach (my_point pp in points)
                if (Math.Abs(pp.X - e.X) <= 3 && Math.Abs(pp.Y - e.Y) <= 3)
                    p = pp;

            // если нажата левая кнопка мыши, то добавляем
            if (e.Button == MouseButtons.Left)
            {
                // если нажата кнопка "центр" для поворота фигуры
                if (is_point_rot)
                {
                    rotate_point = new my_point(e.X, e.Y);
                    g.FillEllipse(brush, rotate_point.X - 4, rotate_point.Y - 4, 8, 8);
                    pictureBox.Image = bmp;
                    is_point_rot = false;
                    return;
                }

                // если точки не было, то рисуем её
                if (p == null)
                {
                    my_point new_p = new my_point(e.X, e.Y);
                    points.Add(new_p);
                    g.FillEllipse(brush, new_p.X - 4, new_p.Y - 4, 8, 8);
                    pictureBox.Image = bmp;
                    select_point_add = null;
                    center_point = centerPoint();
                    return;
                }

                // если точка уже есть и она не центр для поворота, то делаем её началом для ребра
                if (select_point_add == null)
                {
                    select_point_add = p;
                    return;
                }

                // иначе, точка - конец ребра, рисуем ребро
                edge ed = new edge(select_point_add, p);
                select_point_add = null;
                edges.Add(ed);
                g.DrawLine(pen, ed.P1.X, ed.P1.Y, ed.P2.X, ed.P2.Y);
                center_point = centerPoint();
                pictureBox.Image = bmp;
            }

            // иначе - удаляем
            else
            {
                if (p != null)
                {
                    // если точка принадлежит ребру, то рассматриваем удаление ребра
                    if (edges.Any(element => element.contains(p)))
                    {
                        // если не выбрана первая точка удаляемого ребра, то выбираем её
                        if (select_point_del == null)
                        {
                            select_point_del = p;
                            return;
                        }

                        // удаляем ребро
                        edges.RemoveAll(element => element.contains(p) && element.contains(select_point_del));
                        select_point_del = null;
                        redrawImage();
                        return;
                    }

                    // иначе, удаляем просто точку
                    points.Remove(p);
                    select_point_del = null;
                    redrawImage();
                }
            }
        }

        /// <summary>
        /// Перемещение фигуры на заданные сдвиги по Х и У
        /// </summary>
        private void butt_dis_Click(object sender, EventArgs e)
        {
            rotate_point = null;
            int kx = (int)set_dis_x.Value, ky = (int)set_dis_y.Value;
            foreach (my_point p in points)
            {
                p.X += kx;
                p.Y += ky;
            }
            redrawImage();
        }

        /// <summary>
        /// Поворот фигуры на заданный угол относительно заданной точки
        /// </summary>
        private void butt_rot_Click(object sender, EventArgs e)
        {
            if (rotate_point == null)
            {
                MessageBox.Show("Не выбрана точка для поворота фигуры!", "Ошибка", MessageBoxButtons.OK);
                return;
            }
            rotate_figure((int)set_rot_ang.Value);
        }

        private void rotate_figure(int degree)
        {
            double angle = ((double)degree * Math.PI) / 180;
            foreach (my_point p in points)
            {
                p.X -= rotate_point.X;
                p.Y -= rotate_point.Y;
                double xa = p.X * Math.Cos(angle) + p.Y * Math.Sin(angle);
                double ya = p.Y * Math.Cos(angle) - p.X * Math.Sin(angle);
                p.X = (int)(xa + rotate_point.X);
                p.Y = (int)(ya + rotate_point.Y);
            }
            redrawImage();
        }

        /// <summary>
        /// Выбор точки, относительно которой будет поворачиваться фигура
        /// </summary>
        private void butt_set_center_Click(object sender, EventArgs e)
        {
            is_point_rot = true;
        }

        /// <summary>
        /// Изменение масштаба заданной фигуры
        /// </summary>
        private void butt_sc_Click(object sender, EventArgs e)
        {
            rotate_point = null;
            my_point center = centerPoint();
            double kx = (double)set_sc_x.Value;
            double ky = (double)set_sc_y.Value;
            foreach (my_point p in points)
            {
                p.X -= center_point.X;
                p.Y -= center_point.Y;
                p.X = (int)(p.X * kx);
                p.Y = (int)(p.Y * ky);
                p.X += center_point.X;
                p.Y += center_point.Y;
            }
            redrawImage();
        }

        /// <summary>
        /// Очищаем рисунок
        /// </summary>
        private void butt_clear_Click(object sender, EventArgs e)
        {
            g.Clear(Color.White);
            rotate_point = null;
            is_point_rot = false;
            points.Clear();
            edges.Clear();
            pictureBox.Image = bmp;
            set_dis_x.Value = set_dis_y.Value = set_rot_ang.Value = 0;
            set_sc_x.Value = set_sc_y.Value = 1M;
        }

        /// <summary>
        /// Вычисляем координаты центра фигуры
        /// </summary>
        private my_point centerPoint()
        {
            if (points.Count == 0)
                return null;
            int sumX = 0, sumY = 0;
            foreach (my_point p in points)
            {
                sumX += p.X;
                sumY += p.Y;
            }
            sumX /= points.Count;
            sumY /= points.Count;
            return new my_point(sumX, sumY);
        }

        /// <summary>
        /// Перерисовываем pictureBox
        /// </summary>
        private void redrawImage()
        {
            g.Clear(Color.White);
            if (rotate_point != null)
                g.FillEllipse(brush, rotate_point.X - 4, rotate_point.Y - 4, 8, 8);
            foreach (my_point p in points)
                g.FillEllipse(brush, p.X - 4, p.Y - 4, 8, 8);
            foreach (edge e in edges)
                g.DrawLine(pen, e.P1.X, e.P1.Y, e.P2.X, e.P2.Y);
            pictureBox.Image = bmp;
            center_point = centerPoint();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            g = Graphics.FromImage(bmp);
        }

        private void butt_pos_Click(object sender, EventArgs e)
        {
            if ((edges.Count != 1) || (points.Count != 3))
            {
                MessageBox.Show("Нарисуйте только ребро и точку!", "Ошибка!");
                return;
            }

            int z = pos_rel_edge(edges[0], points[2]);
            if (z == -1)
                MessageBox.Show("Точка находится справа от ребра", "Результат");
            else
            if (z == 1)
                MessageBox.Show("Точка находится слева от ребра", "Результат");
            else
                MessageBox.Show("Точка находится на ребре", "Результат");
        }

        /// <summary>
        /// Определяет c какой стороны относительно направленного ребра лежит точка
        /// </summary>
        /// <param name="ed">ребро</param>
        /// <param name="pt">точка</param>
        /// <returns>1 - слева, -1 - справа, 0 - на ребре</returns>
        private int pos_rel_edge(edge ed, my_point pt)
        {
            double z = (ed.P2.Y - ed.P1.Y) * pt.X + (ed.P1.X - ed.P2.X) * pt.Y + (ed.P1.X * (ed.P1.Y - ed.P2.Y) + ed.P1.Y * (ed.P2.X - ed.P1.X));
            if (z < 0)
                return -1;
            else
                if (z > 0)
                return 1;
            else
                return 0;
        }

        private void pos_polygon_Click(object sender, EventArgs e)
        {
            if ((edges.Count < 3) || (points.Count != edges.Count + 1))
            {
                MessageBox.Show("Нарисуйте только многоугольник и точку!", "Ошибка!");
                return;
            }

            my_point prev = edges[0].end();
            for (int i = 1; i < edges.Count; i++)
            {
                if (prev != edges[i].start())
                {
                    MessageBox.Show("Нарисуйте многоугольник правильно!", "Ошибка!");
                    return;
                }
                prev = edges[i].end();
            }
            if (prev != edges[0].start())
            {
                MessageBox.Show("Нарисуйте многоугольник правильно!", "Ошибка!");
                return;
            }
            my_point pt = points[points.Count - 1];
            double sum_degrees = 0;
            foreach (edge ed in edges)
                sum_degrees += degree_between_edges(new edge(pt, ed.start()), new edge(pt, ed.end())) * (-pos_rel_edge(ed, pt));
            if (sum_degrees == 360)
                MessageBox.Show("Точка находится внутри многоугольника", "Положение точки");
            else
                MessageBox.Show("Точка находится вне многоугольника", "Положение точки");
        }
        /// <summary>
        /// Вычисляется абсолютное значение угла между двумя ребрами
        /// </summary>
        /// <param name="e1">Первое ребро</param>
        /// <param name="e2">Второе ребро</param>
        /// <returns>Возвращается значение угла между ребрами в градусах</returns>
        private double degree_between_edges(edge e1, edge e2)
        {
            int e1X = e1.P2.X - e1.P1.X;
            int e1Y = e1.P2.Y - e1.P1.Y;
            int e2X = e2.P2.X - e2.P1.X;
            int e2Y = e2.P2.Y - e2.P1.Y;
            double res = Math.Acos((e1X * e2X + e1Y * e2Y) / (Math.Sqrt(e1X * e1X + e1Y * e1Y) * Math.Sqrt(e2X * e2X + e2Y * e2Y))) * (180 / Math.PI);
            return res;
        }

        private void butt_rot_avg_Click(object sender, EventArgs e)
        {
            if ((edges.Count != 1) || (points.Count != 2))
            {
                MessageBox.Show("Нарисуйте только одно ребро!", "Ошибка!");
                return;
            }
            edge ed = edges[0];
            rotate_point = new my_point(ed.P1.X + (ed.P2.X - ed.P1.X) / 2, ed.P1.Y + (ed.P2.Y - ed.P1.Y) / 2);
            rotate_figure(90);
            rotate_point = null;
        }

        my_point intersection(my_point A, my_point B, my_point C, my_point D)
        {
            double xo = A.X, yo = A.Y;
            double p = B.X - A.X, q = B.Y - A.Y;

            double x1 = C.X, y1 = C.Y;
            double p1 = D.X - C.X, q1 = D.Y - C.Y;

            double x = (xo * q * p1 - x1 * q1 * p - yo * p * p1 + y1 * p * p1) /
                (q * p1 - q1 * p);
            double y = (yo * p * q1 - y1 * p1 * q - xo * q * q1 + x1 * q * q1) /
                (p * q1 - p1 * q);

            return new my_point((int)Math.Round(x), (int)Math.Round(y));
        }

        private void butt_intersec_Click(object sender, EventArgs e)
        {
            if ((edges.Count != 2) || (points.Count != 4))
            {
                MessageBox.Show("Нарисуйте только два ребра!", "Ошибка!");
                return;
            }
            edge e1 = edges[0];
            edge e2 = edges[1];
            my_point its = intersection(e1.start(), e1.end(), e2.start(), e2.end());
            if ((its.X >= Math.Min(e1.P1.X, e1.P2.X)) && (its.X <= Math.Max(e1.P1.X, e1.P2.X)) &&
                (its.X >= Math.Min(e2.P1.X, e2.P2.X)) && (its.X <= Math.Max(e2.P1.X, e2.P2.X)) &&
                (its.Y >= Math.Min(e1.P1.Y, e1.P2.Y)) && (its.Y <= Math.Max(e1.P1.Y, e1.P2.Y)) &&
                (its.Y >= Math.Min(e2.P1.Y, e2.P2.Y)) && (its.Y <= Math.Max(e2.P1.Y, e2.P2.Y)))
            {
                g.FillEllipse(new SolidBrush(Color.Black), its.X - 4, its.Y - 4, 8, 8);
                pictureBox.Refresh();
            }
            else
                MessageBox.Show("Ребра не пересекаются!", "Сообщение");
        }
    }

    public class my_point
    {
        public int X, Y;
        public my_point(int x, int y) { X = x; Y = y; }
        public static bool operator ==(my_point p1, my_point p2)
        {
            if (System.Object.ReferenceEquals(p1, p2))
                return true;
            if (((object)p1 == null) || ((object)p2 == null))
                return false;
            return p1.X == p2.X && p1.Y == p2.Y;
        }
        public static bool operator !=(my_point p1, my_point p2)
        {
            return !(p1 == p2);
        }
    }

    public class edge
    {
        public my_point P1, P2;
        public edge(my_point p1, my_point p2) { P1 = p1; P2 = p2; }
        public bool contains(my_point p) { return p == P1 || p == P2; }
        public my_point start() { return P1; }
        public my_point end() { return P2; }
    }
}
